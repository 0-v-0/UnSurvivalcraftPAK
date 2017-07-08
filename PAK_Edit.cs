using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Engine.Content;
using Engine.Media;
using Engine;
using Engine.Serialization;
using Engine.Audio;

namespace PCSCPacker
{
    public class PAK_Edit
    {

        private static string dir;
        public static void unPAK(string file)
        {
            if (!File.Exists(file))
            {
                Console.WriteLine("文件不存在！");
                return;
            }
            string text = file.Substring(0, file.Length - 4);
            dir = text;
            if (!Directory.Exists(text))
                Directory.CreateDirectory(text);
            ContentEdit contentEdit = new ContentEdit();
            contentEdit.AddPackage(file);
            if (!contentEdit.AddPackage(file))
            {
                Console.WriteLine("解包失败");
                return;
            }
            Console.WriteLine("开始解包文件");
            foreach (ContentInfo current in contentEdit.List())
            {
                string text2 = text + '/' + current.Name;
                ContentEdit.ContentDescription contentDescription;
                contentEdit.m_contentDescriptionsByName.TryGetValue(current.Name, out contentDescription);
                Stream stream = contentEdit.PrepareContentStream(contentDescription);
                FileStream fileStream;
                try
                {
                    fileStream = File.Create(text2);
                }
                catch (DirectoryNotFoundException)
                {
                    string path = text2.TrimEnd("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_".ToCharArray());
                    Directory.CreateDirectory(path);
                    fileStream = File.Create(text2);
                }
                stream.CopyTo(fileStream);
                stream.Close();
                fileStream.Close();
                UnpackData(text2, current.TypeName);
                Console.WriteLine(current.Name);
            }
            contentEdit.Dispose();
            Console.WriteLine("文件解包完成");
        }
        public static void PAK(string directory)
        {
            if (!Directory.Exists(directory))
            {
                Console.WriteLine("PAK包文件夹不存在");
                return;
            }
            List<ContentInfo> list = new List<ContentInfo>();
            list = Files(list,directory);
            List<long> list2 = new List<long>();
            new DirectoryInfo(directory);
            if (directory.EndsWith("/") || directory.EndsWith("\\"))
            {
                directory = directory.Substring(0, directory.Length - 1);
            }
            FileStream fileStream;
            if (File.Exists(directory + ".pak"))
            {
                if (File.Exists(directory + "_sign.pak"))
                    fileStream = new FileStream(directory + "_sign.pak", FileMode.Open);
                else
                    fileStream = new FileStream(directory + "_sign.pak", FileMode.Create);
            }else
            {
                fileStream = new FileStream(directory + ".pak", FileMode.Create);
            }
            EngineBinaryWriter binaryWriter = new EngineBinaryWriter(fileStream,true);
            binaryWriter.Write((byte)80);
            binaryWriter.Write((byte)65);
            binaryWriter.Write((byte)75);
            binaryWriter.Write((byte)0);
            binaryWriter.Write(0);
            binaryWriter.Write(list.Count);
            foreach (ContentInfo current in list)
            {
                binaryWriter.Write(current.Name.Substring(directory.Length + 1, current.Name.Length - directory.Length - 1));
                binaryWriter.Write(current.TypeName);
                list2.Add(binaryWriter.BaseStream.Position);
                binaryWriter.Write(0);
                binaryWriter.Write(0);
            }
            long position = binaryWriter.BaseStream.Position;
            long num = position;
            binaryWriter.Seek(4, SeekOrigin.Begin);
            binaryWriter.Write((int)position);
            int num2 = 0;
            int num3 = 0;
            long num4 = 0L;
            foreach (ContentInfo current2 in list)
            {
                binaryWriter.Seek((int)num, SeekOrigin.Begin);
                binaryWriter.Write((byte)222);
                binaryWriter.Write((byte)173);
                binaryWriter.Write((byte)190);
                binaryWriter.Write((byte)239);
                num = binaryWriter.BaseStream.Position;
                num3 = (int)num;
                Stream stream = PackData(current2.Name, current2.TypeName);
                fileStream.Seek(binaryWriter.BaseStream.Position, SeekOrigin.Begin);
                stream.CopyTo(fileStream);
                num4 = stream.Position;
                stream.Close();
                num = binaryWriter.BaseStream.Position;
                binaryWriter.Seek((int)list2[num2++], SeekOrigin.Begin);
                binaryWriter.Write((int)((long)num3 - position));
                binaryWriter.Write((int)num4);
                Console.WriteLine(current2.Name);
            }
            binaryWriter.Close();
            fileStream.Close();
            Console.WriteLine("打包完成");
        }
        private static List<ContentInfo> Files(List<ContentInfo> list,string directory)
        {
            foreach (string dire in Directory.GetDirectories(directory))
            {
                list = Files(list, dire);
            }
            foreach (string file in Directory.GetFiles(directory))
            {
                string extenName = Path.GetExtension(file);
                string typeName;
                switch (extenName)
                {
                    case ".txt":
                        typeName = "System.String";
                        break;
                    case ".xml":
                        typeName = "System.Xml.Linq.XElement";
                        break;
                    case ".png":
                        typeName = "Engine.Graphics.Texture2D";
                        break;
                    case ".png？":
                        typeName = "Engine.Graphics.Texture2D";
                        break;
                    case ".dae？":
                        typeName = "Engine.Graphics.Model";
                        break;
                    case ".shader？":
                        typeName = "Engine.Graphics.Shader";
                        break;
                    case ".font？":
                        typeName = "Engine.Media.BitmapFont";
                        break;
                    case ".wav":
                        typeName = "Engine.Audio.SoundBuffer";
                        break;
                    case ".wav？":
                        typeName = "Engine.Audio.SoundBuffer";
                        break;
                    case ".ogg":
                        typeName = "Engine.Media.StreamingSource";
                        break;
                    default:
                        typeName = "File";
                        break;
                }
                ContentInfo item;
                if (typeName == "") continue;
                string Dire = Path.GetDirectoryName(file);
                string[] Arratitem = Dire.Split((char)'\\');
                if (Arratitem.Length > 1)
                {
                    Dire = Arratitem[0];
                    for(int i = 1; i < Arratitem.Length; i++)
                    {
                        Dire += "/" + Arratitem[i];
                    }
                }

                if (typeName == "File")
                {
                    item.Name = Dire + "/" + Path.GetFileName(file);
                }
                else
                {
                    item.Name = Dire + "/" + Path.GetFileNameWithoutExtension(file);
                }
                item.TypeName = typeName;
                list.Add(item);
            }
            return list;
        }
        public static void UnpackData(string text, string typeName)
        {
            if (typeName == "System.String")
            {
                FileStream input = File.OpenRead(text);
                EngineBinaryReader binaryReader = new EngineBinaryReader(input);
                binaryReader.BaseStream.Position = 0;
                string s = binaryReader.ReadString();
                binaryReader.Close();
                FileStream fileStream = File.Create(text + ".txt");
                fileStream.Write(Encoding.UTF8.GetBytes(s), 0, Encoding.UTF8.GetBytes(s).Length);
                fileStream.Close();
                File.Delete(text);
                return;
            }
            if (typeName == "System.Xml.Linq.XElement")
            {
                FileStream input2 = File.OpenRead(text);
                EngineBinaryReader binaryReader2 = new EngineBinaryReader(input2);
                binaryReader2.BaseStream.Position = 0;
                string s2 = binaryReader2.ReadString();
                binaryReader2.Close();
                FileStream fileStream2 = File.Create(text + ".xml");
                fileStream2.Write(Encoding.UTF8.GetBytes(s2), 0, Encoding.UTF8.GetBytes(s2).Length);
                fileStream2.Close();
                File.Delete(text);
                return;
            }
            if (typeName == "Engine.Graphics.Texture2D")
            {
                FileStream stream = File.OpenRead(text);
                EngineBinaryReader engineBinaryReader = new EngineBinaryReader(stream, false);
                engineBinaryReader.ReadByte();
                int width = engineBinaryReader.ReadInt32();
                int height = engineBinaryReader.ReadInt32();
                Image image = new Image(width, height);
                engineBinaryReader.ReadInt32();
                for (int i = 0; i < image.Pixels.Length; i++)
                {
                    image.Pixels[i] = engineBinaryReader.ReadColor();
                }
                engineBinaryReader.Close();
                File.Delete(text);
                FileStream fileStream3 = File.Create(text + ".png");
                Png.Save(image, fileStream3, Png.Format.RGBA8);
                fileStream3.Close();
                /*FileStream fileStream = File.OpenRead(text);
                FileStream fileStream2 = File.Create(text + ".png");
                fileStream.ReadByte();
                Image image = ImageContentReader.ReadImage(fileStream);
                Png.Save(image,fileStream2,Png.Format.RGBA8);
                fileStream2.Close();
                fileStream.Close();
                File.Delete(text);*/
            }
            if (typeName == "Engine.Audio.SoundBuffer")
            {
                FileStream input = File.OpenRead(text);
                FileStream fileStream = File.Create(text + ".wav？");
                input.CopyTo(fileStream);
                input.Close();
                fileStream.Close();
                File.Delete(text);
                /*ContentStream stream = new ContentStream(text);
                FileStream fileStream = File.Create(text + ".wav");
                SoundBuffer data = Read(stream,null);
                int ad = data.SamplesCount;
                Console.WriteLine(ad);
                SoundData sounddata = new SoundData((int)data.ChannelsCount,(int)data.SamplingFrequency,ad);
                Wav.Save(sounddata, fileStream);
                stream.Close();
                fileStream.Close();
                File.Delete(text);*/
            }
            if (typeName == "Engine.Graphics.Model")
            {



                FileStream stream = File.OpenRead(text);
                if (Program.size != 0f && Program.size > 1f && Program.size < 255f)
                {
                    string texts = dir + $"\\x{Program.size}Model\\";
                    if (!Directory.Exists(texts))
                        Directory.CreateDirectory(texts);
                    string name = Path.GetFileNameWithoutExtension(text);
                    //Console.WriteLine(texts + name + Program.size + "x.dae？");
                    FileStream i = File.Create(texts+name + Program.size + "x.dae？");
                    bool keepSourceVertexDataInTags = new BinaryReader(stream).ReadBoolean();
                    new BinaryWriter(i).Write(keepSourceVertexDataInTags);
                    ModelData j = ModelDataContentReader.ReadModelData(stream);
                    ModelDataContentWriter w = new ModelDataContentWriter();
                    w.ModelData = text;
                    Vector3 scale = default(Vector3);
                    if (scale == default(Vector3))
                    {
                        scale = new Vector3(Program.size, Program.size, Program.size);
                    }
                    ModelDataContentWriter.WriteModelData(i, j, scale);
                    i.Close();
                }
                FileStream fileStream = File.Create(text + ".dae？");
                stream.CopyTo(fileStream);
                stream.Close();
                fileStream.Close();
                File.Delete(text);


                /*FileStream input = File.OpenRead(text);
                FileStream fileStream = File.Create(text + ".dae？");
                input.CopyTo(fileStream);
                input.Close();
                fileStream.Close();
                File.Delete(text);*/
            }
            if (typeName == "Engine.Graphics.Shader")
            {
                FileStream input = File.OpenRead(text);
                FileStream fileStream = File.Create(text + ".shader？");
                input.CopyTo(fileStream);
                input.Close();
                fileStream.Close();
                File.Delete(text);
                return;
            }
            if (typeName == "Engine.Media.BitmapFont")
            {
                FileStream input = File.OpenRead(text);
                FileStream fileStream = File.Create(text + ".font？");
                input.CopyTo(fileStream);
                input.Close();
                fileStream.Close();
                File.Delete(text);
                return;
            }
            if (typeName == "Engine.Media.StreamingSource")
            {
                FileStream input = File.OpenRead(text);
                FileStream fileStream = File.Create(text + ".ogg");
                input.CopyTo(fileStream);
                input.Close();
                fileStream.Close();
                File.Delete(text);
                return;
            }
        }
        public static Stream PackData(string text, string typeName)
        {
            MemoryStream memoryStream = new MemoryStream();
            if (typeName == "System.String")
            {
                FileStream fileStream = File.OpenRead(text + ".txt");
                byte[] array = new byte[fileStream.Length];
                fileStream.Read(array, 0, (int)fileStream.Length);
                fileStream.Close();
                BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
                binaryWriter.Write(Encoding.UTF8.GetString(array));
            }
            else if (typeName == "System.Xml.Linq.XElement")
            {
                FileStream fileStream2 = File.OpenRead(text + ".xml");
                byte[] array2 = new byte[fileStream2.Length];
                fileStream2.Read(array2, 0, (int)fileStream2.Length);
                fileStream2.Close();
                BinaryWriter binaryWriter2 = new BinaryWriter(memoryStream);
                binaryWriter2.Write(Encoding.UTF8.GetString(array2));
            }
            else if (typeName == "Engine.Graphics.Texture2D")
            {
                if (!File.Exists(text + ".png"))
                {
                    if (!File.Exists(text + ".png？"))
                    {
                        throw new InvalidOperationException("发生图片加载错误!!!!!!!");
                    }
                    FileStream fileStream = File.OpenRead(text + ".png？");
                    fileStream.CopyTo(memoryStream);
                    fileStream.Close();
                }
                else
                {
                    FileStream fileStream3 = File.OpenRead(text + ".png");
                    Image image = Png.Load(fileStream3);
                    fileStream3.Close();
                    if (!MathUtils.IsPowerOf2(image.Width) || !MathUtils.IsPowerOf2(image.Height))
                    {
                        TextureContentWriter.WriteTexture(memoryStream, image, false, false, true);
                    }
                    else
                    {
                        TextureContentWriter.WriteTexture(memoryStream, image, true, false, true);
                    }
                }
            }
            else if (typeName == "Engine.Audio.SoundBuffer")
            {
                if (!File.Exists(text + ".wav"))
                {
                    if (!File.Exists(text + ".wav？"))
                    {
                        throw new InvalidOperationException("音频文件加载错误!!!!!!!");
                    }
                    FileStream fileStream = File.OpenRead(text + ".wav？");
                    fileStream.CopyTo(memoryStream);
                    fileStream.Close();
                }
                else
                {
                    FileStream fileStream3 = File.OpenRead(text + ".wav");
                    StreamingSource wav = Wav.Stream(fileStream3);
                    SoundBufferContentWriter.WritePcm(memoryStream,wav);
                    fileStream3.Close();
                }
            }
            else if (typeName == "Engine.Graphics.Model")
            {
                FileStream fileStream = File.OpenRead(text + ".dae？");
                fileStream.CopyTo(memoryStream);
                fileStream.Close();
            }
            else if (typeName == "Engine.Graphics.Shader")
            {
                FileStream fileStream = File.OpenRead(text + ".shader？");
                fileStream.CopyTo(memoryStream);
                fileStream.Close();
            }
            else if (typeName == "Engine.Media.BitmapFont")
            {
                FileStream fileStream = File.OpenRead(text + ".font？");
                fileStream.CopyTo(memoryStream);
                fileStream.Close();
            }
            else if (typeName == "Engine.Media.StreamingSource")
            {
                FileStream fileStream = File.OpenRead(text + ".ogg");
                fileStream.CopyTo(memoryStream);
                fileStream.Close();
            }
            else
            {
                FileStream fileStream4 = File.OpenRead(text);
                fileStream4.CopyTo(memoryStream);
                fileStream4.Close();
            }
            memoryStream.Position = 0L;
            return memoryStream;
        }

        /*public static SoundBuffer Read(ContentStream stream, object existingObject)
        {
            if (existingObject == null)
            {
                BinaryReader expr_0C = new BinaryReader(stream);
                bool flag = expr_0C.ReadBoolean();
                int channelsCount = expr_0C.ReadInt32();
                int samplingFrequency = expr_0C.ReadInt32();
                int bytesCount = expr_0C.ReadInt32();
                if (flag)
                {
                    MemoryStream memoryStream = new MemoryStream();
                    using (Engine.Media.StreamingSource streamingSource = Engine.Media.Ogg.Stream(stream, false))
                    {
                        streamingSource.CopyTo(memoryStream);
                        if (memoryStream.Length > 2147483647L)
                        {
                            throw new InvalidOperationException("Audio data too long.");
                        }
                        memoryStream.Position = 0L;
                        Console.WriteLine("1111111111");

                        return new Engine.Audio.SoundBuffer(memoryStream, (int)memoryStream.Length, channelsCount, samplingFrequency);
                    }
                }
                Console.WriteLine("22222222222222");
                return new Engine.Audio.SoundBuffer(stream, bytesCount, channelsCount, samplingFrequency);
            }
            throw new NotSupportedException();
        }*/


    }


}