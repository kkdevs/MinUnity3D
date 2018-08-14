using LZ4;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

static class Program
{
	public static Form1 ui;

	public static string path;
	[STAThread]
	static void Main(string[] args)
	{
		bool randguid = false;
		bool silent = false;
		bool doexit = false;
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);
		while (args.Length > 0)
		{
			if (args[0] == "/silent")
				silent = true;
			else if (args[0] == "/exit")
				doexit = true;
			else if (args[0] == "/randguid")
				randguid = true;
			else break;
			args = args.Skip(1).ToArray();
		}

		if (args.Length == 0)
		{
			var browse = new FolderBrowserDialog();
			browse.SelectedPath = MinUnity3D.Properties.Settings.Default.folder;
			if (browse.ShowDialog() != DialogResult.OK)
				return;
			MinUnity3D.Properties.Settings.Default.folder = path = browse.SelectedPath;
			MinUnity3D.Properties.Settings.Default.Save();
		}
		else path = args[0];
		if (!Directory.Exists(path))
			return;

		ui = new Form1();
		if (!silent)
		{
			ui.Show();
		} else
		{
			ui.WindowState = FormWindowState.Minimized;
			ui.ShowInTaskbar = false;
		}
		long pending = 0;
		bool closing = false;
		Action process = ()=> {
			long compressed = 0;
			long processed = 0;
			ui.TopMost = false;
			long nfiles = 0;
			long done = 0;
			foreach (var f in Directory.GetFiles(path, "*.unity3d", SearchOption.AllDirectories))
			{
				if (f.ToLower().Contains("\\sound\\"))
					continue;
				nfiles++;
				pending++;
				ThreadPool.QueueUserWorkItem((fo) =>
				{
					while (!closing)
					{
						lock (nblock)
						{
							if (nbufs < 1L * 1024 * 1024 * 1024)
								break;
						}
						Thread.Sleep(5000);
					}
					var fn = fo as string;
					if (!closing)
					{
						long res = -1;
						var fno = fn + ".comp";
						long outlen;
						long inlen = new FileInfo(fn).Length;
						lock (nblock)
						{
							nbufs += inlen;
						}
						using (var fin = File.Open(f, FileMode.Open))
						using (var fout = File.Create(fno))
						{
							res = Repack(fin, fout, randguid);
							outlen = fout.Position;
						}

#if true
						if (res != -1)
							File.Replace(fno, f, null);
						else
							File.Delete(fno);
#endif
						lock (nblock)
						{
							nbufs -= inlen;
						}

						ui.Invoke(new MethodInvoker(delegate ()
						{
							processed += res;
							compressed += outlen;
							done++;
							double GB = 1024 * 1024 * 1024;
							ui.textProgress.Text = $"{done} of {nfiles}, {done * 100.0 / nfiles:0.00}% done";
							ui.textRatio.Text = $"{processed / GB:0.00}GB => {compressed / GB:0.00}GB, {compressed * 100.0 / processed:0.00}% ratio";
							ui.progressFile.Value = Math.Min((int)(done  * 100 / nfiles), 100);
							ui.progressRatio.Value = Math.Min((int)(compressed * 100 / processed), 100);
							ui.log.AppendText($"{f} {(outlen * 100.0 / res):0.00}%\n");
							if (closing)
								ui.log.AppendText($"*** terminating, {pending} jobs remaining\n");
						}));
					}
					
					ui.Invoke(new MethodInvoker(delegate ()
					{
						if (--pending == 0)
						{
							if (closing)
								ui.Close();
							else
							{
								ui.log.AppendText("All done.");
								if (silent || doexit)
									Application.Exit();
							}
						}
							
					}));
				}, f);
			}
		};
		ui.FormClosing += (o, e) =>
		{
			closing = true;
			if (pending > 0)
				e.Cancel = true;
		};
		if (!silent)
			ui.Shown += (o, e) =>
			{
				process();
			};
		else
			process();

		Application.Run(ui);
	}

	public static uint bswap(uint x)
	{
		x = (x >> 16) | (x << 16);
		return ((x & 0xFF00FF00) >> 8) | ((x & 0x00FF00FF) << 8);
	}
	public static ulong bswap(ulong x)
	{
		x = (x >> 32) | (x << 32);
		x = ((x & 0xFFFF0000FFFF0000) >> 16) | ((x & 0x0000FFFF0000FFFF) << 16);
		return ((x & 0xFF00FF00FF00FF00) >> 8) | ((x & 0x00FF00FF00FF00FF) << 8);
	}
	public static void Put(this BinaryWriter w, string s)
	{
		w.Write(Encoding.UTF8.GetBytes(s));
		w.Write((byte)0);
	}
	public static string GetString(this BinaryReader r)
	{
		var sb = new StringBuilder();
		char c;
		while ((c = r.ReadChar()) != 0)
			sb.Append(c);
		return sb.ToString();
	}
	public static void Put(this BinaryWriter w, int v) => w.Write(bswap((uint)v));
	public static void Put(this BinaryWriter w, long v) => w.Write(bswap((ulong)v));
	public static void Put(this BinaryWriter w, short v) => w.Write((short)(bswap((uint)v) >> 16));
	public static int GetInt(this BinaryReader r) => (int)bswap(r.ReadUInt32());
	public static short GetShort(this BinaryReader r) => (short)(bswap(r.ReadUInt16()) >> 16);
	public static long GetLong(this BinaryReader r) => (long)bswap(r.ReadUInt64());
	public static long nbufs;
	public static object nblock = new object();
	public static long Repack(Stream input, Stream output, bool randomize = false, int lz4blockSize = 128 * 1024)
	{
		long unsize = 0;
		const int minRatio = 95;
		var baseStart = output.Position;
		var r = new BinaryReader(input, Encoding.ASCII);
		var w = new BinaryWriter(output, Encoding.ASCII);

		var format = r.GetString();
		if (format != "UnityFS")
			return -1;
		w.Put(format);

		var gen = r.GetInt();
		if (gen != 6)
			return -1;
		w.Put(gen);

		w.Put(r.GetString());
		w.Put(r.GetString());

		// defer
		var infoPos = w.BaseStream.Position;
		w.BaseStream.Position += 16; // bundlesize + metacomp + metauncomp

		var bundleSize = r.GetLong();
		var metaCompressed = r.GetInt();
		var metaUncompressed = r.GetInt();
		unsize += metaUncompressed;
		var flags = r.GetInt();
		w.Put(0x43);
		var dataPos = r.BaseStream.Position;

		if ((flags & 0x80) != 0)
			r.BaseStream.Position = bundleSize - metaCompressed;
		else
			dataPos += metaCompressed;

		byte[] metabuf = null;
		switch (flags & 0x3f)
		{
			case 3:
			case 2:
				metabuf = LZ4Codec.Decode(r.ReadBytes(metaCompressed), 0, metaCompressed, metaUncompressed);
				break;
			case 0:
				metabuf = r.ReadBytes(metaUncompressed);
				break;
			default:
				return -1;
		}

		r.BaseStream.Position = dataPos;
		var meta = new BinaryReader(new MemoryStream(metabuf), Encoding.ASCII);
		var newmeta = new BinaryWriter(new MemoryStream(), Encoding.ASCII);
		newmeta.BaseStream.Position += 16 + 4; // +4 for pending.Length
		meta.BaseStream.Position += 16;
		int nblocks = meta.GetInt();
		List<byte[]> pending = new List<byte[]>();
		var tbuf = new byte[0];
		int tbufptr = 0;
		for (var i = 0; i < nblocks; i++)
		{
			Console.WriteLine(i);
			var origSize = meta.GetInt();
			var compSize = meta.GetInt();
			var blockFlags = meta.GetShort() & 0x3f;
			var block = r.ReadBytes(compSize);
			unsize += origSize;
			bool flush = false;
			if (blockFlags == 0 || blockFlags == 2 || blockFlags == 3)
			{
				if (tbuf.Length < tbufptr + origSize)
					Array.Resize(ref tbuf, tbufptr + origSize);
				if (blockFlags != 0)
				{
					LZ4Codec.Decode(block, 0, compSize, tbuf, tbufptr, origSize);
				}
				else
				{
					Buffer.BlockCopy(block, 0, tbuf, tbufptr, origSize);
				}
				tbufptr += origSize;
				if ((i < nblocks - 1) && (tbufptr < lz4blockSize))
					continue;
			}
			else flush = true;		
			if (tbufptr > 0) { // full block, last block, or non-compressible encountered
				for (int pos = 0; pos < tbufptr; pos += lz4blockSize)
				{
					var orig = Math.Min(lz4blockSize, tbufptr - pos);
					byte[] newblock;
					short nbf;
					try
					{
						newblock = LZ4Codec.EncodeHC(tbuf, pos, orig);
						nbf = (short)3;
					} catch
					{
						newblock = LZ4Codec.Encode(tbuf, pos, orig);
						nbf = (short)3;
					}
					if (newblock.Length * 100 > orig * minRatio)
					{
						newblock = new byte[orig];
						Array.Copy(tbuf, pos, newblock, 0, orig);
						nbf = 0;
					}
					newmeta.Put(orig);
					newmeta.Put(newblock.Length); ;
					newmeta.Put(nbf);
					pending.Add(newblock);
				}
				tbufptr = 0;
			}
			if (flush)
			{
				newmeta.Put(origSize);
				newmeta.Put(compSize);
				newmeta.Put(blockFlags);
				pending.Add(block);
			}
		}

		//Console.WriteLine(pending.Count);
		int nfiles = meta.GetInt();
		newmeta.Put(nfiles);
		var rng = new RNGCryptoServiceProvider();
		for (int i = 0; i < nfiles; i++)
		{
			newmeta.Put(meta.GetLong());
			newmeta.Put(meta.GetLong());
			newmeta.Put(meta.GetInt());
			var name = meta.GetString();
			if (randomize)
			{
				var rnbuf = new byte[16];
				rng.GetBytes(rnbuf);
				name = "CAB-" + string.Concat(rnbuf.Select((x) => ((int)x).ToString("X2")).ToArray()).ToLower();
			}
			newmeta.Put(name);
		}
		newmeta.BaseStream.Position = 16;
		newmeta.Put(pending.Count);
		var newmetabuf = (newmeta.BaseStream as MemoryStream).ToArray();
		var newmetabufc = LZ4Codec.EncodeHC(newmetabuf, 0, newmetabuf.Length);
		w.Write(newmetabufc);
		foreach (var buf in pending)
		{
			w.Write(buf);
		}
		var endpos = w.BaseStream.Position;
		var bundlesize = endpos - baseStart;
		w.BaseStream.Position = infoPos;
		w.Put(bundlesize);
		w.Put(newmetabufc.Length);
		w.Put(newmetabuf.Length);
		output.Position = endpos;
		return unsize;
	}
}
