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
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);

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
		ui.Show();
		ui.Shown += (o,e) => {
			ui.TopMost = false;
			new Thread(processFiles).Start();
		};
		ui.FormClosing += (o, e) =>
		{
			closing = true;
			if (processing)
				e.Cancel = true;
		};

		Application.Run(ui);
	}
	public static bool processing;
	public static bool closing;

	public static void processFiles()
	{
		processing = true;
		var allfiles = Directory.GetFiles(path, "*.unity3d", SearchOption.AllDirectories);
		var total = allfiles.Sum(x => new FileInfo(x).Length) + 1;

		var chunk = allfiles.Length / Environment.ProcessorCount;
		if (chunk == 0)
			chunk++;
		long processed = 1;
		long compressed = 1;
		List<Thread> threads = new List<Thread>();
		while (allfiles != null)
		{
			var part = allfiles;
			if (part.Length > chunk)
			{
				part = part.Take(chunk).ToArray();
				allfiles = allfiles.Skip(chunk).ToArray();
			} else
			{
				allfiles = null;
			}
			threads.Add(new Thread(() =>
			{
				foreach (var f in part)
				{
					if (closing)
						break;
					var res = false;
					var fno = f + ".comp";
					long outlen, inlen;
					using (var fin = File.Open(f, FileMode.Open))
					using (var fout = File.Create(fno))
					{
						res = Repack(fin, fout);
						inlen = fin.Seek(0, SeekOrigin.End);
						outlen = fout.Position;
					}
					ui.Invoke(new MethodInvoker(delegate ()
					{
						processed += inlen;
						compressed += outlen;
						double GB = 1024 * 1024 * 1024;
						ui.textProgress.Text = $"{processed / GB:0.00}GB of {total / GB:0.00}GB, {processed * 100.0 / total:0.00}% done";
						ui.textRatio.Text = $"{processed / GB:0.00}GB => {compressed / GB:0.00}GB, {compressed * 100.0 / processed:0.00}% ratio";
						ui.progressFile.Value = Math.Min((int)(processed * 100 / total), 100);
						ui.progressRatio.Value = Math.Min((int)(compressed * 100 / processed), 100);
						ui.log.AppendText($"{f} {(outlen * 100 / inlen):0.00}%\n");
					}));

					//if (res)
					//	File.Replace(fno, f, null);
				}
			}));
			threads.Last().Start();
		}
		foreach (var t in threads)
			t.Join();
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

	public static bool Repack(Stream input, Stream output, bool randomize = false, int lz4blockSize = 128 * 1024)
	{
		const int minRatio = 95;
		var baseStart = output.Position;
		var r = new BinaryReader(input, Encoding.ASCII);
		var w = new BinaryWriter(output, Encoding.ASCII);

		var format = r.GetString();
		if (format != "UnityFS")
			return false;
		w.Put(format);

		var gen = r.GetInt();
		if (gen != 6)
			return false;
		w.Put(gen);

		w.Put(r.GetString());
		w.Put(r.GetString());

		// defer
		var infoPos = w.BaseStream.Position;
		w.BaseStream.Position += 16; // bundlesize + metacomp + metauncomp

		var bundleSize = r.GetLong();
		var metaCompressed = r.GetInt();
		var metaUncompressed = r.GetInt();
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
				return false;
		}

		r.BaseStream.Position = dataPos;
		var meta = new BinaryReader(new MemoryStream(metabuf), Encoding.ASCII);
		var newmeta = new BinaryWriter(new MemoryStream(), Encoding.ASCII);
		newmeta.BaseStream.Position += 16 + 4; // +4 for pending.Length
		meta.BaseStream.Position += 16;
		int nblocks = meta.GetInt();
		List<byte[]> pending = new List<byte[]>();
		for (var i = 0; i < nblocks; i++)
		{
			Console.WriteLine(i);
			var origSize = meta.GetInt();
			var compSize = meta.GetInt();
			var blockFlags = meta.GetShort();
			var block = r.ReadBytes(compSize);
			if (blockFlags == 0x40 || blockFlags == 2 || blockFlags == 3)
			{
				if (blockFlags != 0x40)
					block = LZ4Codec.Decode(block, 0, compSize, origSize);
				for (int pos = 0; pos < block.Length; pos += lz4blockSize)
				{
					var orig = Math.Min(lz4blockSize, block.Length - pos);
					var newblock = LZ4Codec.EncodeHC(block, pos, orig);
					blockFlags = (short)3;
					if (newblock.Length * 100 > orig * minRatio)
					{
						newblock = new byte[orig];
						Array.Copy(block, pos, newblock, 0, orig);
						blockFlags = 0x40;
					}
					newmeta.Put(orig);
					newmeta.Put(newblock.Length); ;
					newmeta.Put(blockFlags);
					pending.Add(newblock);
				}
			}
			else
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
		for (int i = 0; i < nfiles; i++)
		{
			newmeta.Put(meta.GetLong());
			newmeta.Put(meta.GetLong());
			newmeta.Put(meta.GetInt());
			var name = meta.GetString();
			newmeta.Put(name);
		}
		newmeta.BaseStream.Position = 16;
		newmeta.Put(pending.Count);
		var newmetabuf = (newmeta.BaseStream as MemoryStream).ToArray();
		var newmetabufc = LZ4Codec.EncodeHC(newmetabuf, 0, newmetabuf.Length);
		w.Write(newmetabufc);
		foreach (var buf in pending)
			w.Write(buf);
		var endpos = w.BaseStream.Position;
		var bundlesize = endpos - baseStart;
		w.BaseStream.Position = infoPos;
		w.Put(bundlesize);
		w.Put(newmetabufc.Length);
		w.Put(newmetabuf.Length);
		output.Position = endpos;
		return true;
	}
}
