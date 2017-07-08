using Engine;
using Engine.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace PCSCPacker
{
    public class ContentEdit
    {
        public struct ContentDescription
        {
            public string TypeName;

            public ContentStream Stream;

            public int Position;

            public int BytesCount;
        }

        public object m_lock = new object();

        public Dictionary<string, IContentReader> m_contentReadersByTypeName = new Dictionary<string, IContentReader>();

        public HashSet<Assembly> m_scannedAssemblies = new HashSet<Assembly>();

        public Dictionary<string, object> m_contentByName = new Dictionary<string, object>();

        public Dictionary<string, ContentEdit.ContentDescription> m_contentDescriptionsByName = new Dictionary<string, ContentEdit.ContentDescription>();

        public List<ContentInfo> m_contentInfos = new List<ContentInfo>();

        public Dictionary<string, List<ContentInfo>> m_contentInfosByFolder = new Dictionary<string, List<ContentInfo>>();

        public List<ContentInfo> m_emptyContentInfos = new List<ContentInfo>(0);

        public bool AddPackage(string path)
        {
            lock (this.m_lock)
            {
                ContentStream contentStream = new ContentStream(path);
                using (BinaryReader binaryReader = new BinaryReader(contentStream, Encoding.UTF8, true))
                {
                    byte[] array = new byte[4];
                    if (binaryReader.Read(array, 0, array.Length) != array.Length || array[0] != 80 || array[1] != 65 || array[2] != 75 || array[3] != 0)
                    {
                        return false;
                    }
                    int num = binaryReader.ReadInt32();
                    int num2 = binaryReader.ReadInt32();
                    for (int i = 0; i < num2; i++)
                    {
                        //Console.WriteLine(binaryReader.BaseStream.Position + "...");
                        string text = binaryReader.ReadString();
                        //Console.WriteLine(binaryReader.BaseStream.Position + "...");
                        string typeName = binaryReader.ReadString();
                        int position = binaryReader.ReadInt32() + num;
                        int bytesCount = binaryReader.ReadInt32();
                        this.m_contentDescriptionsByName[text] = new ContentEdit.ContentDescription
                        {
                            TypeName = typeName,
                            Stream = contentStream,
                            Position = position,
                            BytesCount = bytesCount
                        };
                        int num3 = text.LastIndexOf('/');
                        string key = (num3 >= 0) ? text.Substring(0, num3) : string.Empty;
                        List<ContentInfo> list;
                        if (!this.m_contentInfosByFolder.TryGetValue(key, out list))
                        {
                            list = new List<ContentInfo>();
                            this.m_contentInfosByFolder.Add(key, list);
                        }
                        list.Add(new ContentInfo
                        {
                            Name = text,
                            TypeName = typeName
                        });
                        this.m_contentInfos.Add(new ContentInfo
                        {
                            Name = text,
                            TypeName = typeName
                        });
                    }
                }
            }
            return true;
        }

        public ReadOnlyList<ContentInfo> List()
        {
            return new ReadOnlyList<ContentInfo>(this.m_contentInfos);
        }
        public void Dispose()
        {
            object @lock = this.m_lock;
            lock (@lock)
            {
                using (Dictionary<string, object>.ValueCollection.Enumerator enumerator = this.m_contentByName.Values.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        IDisposable disposable = enumerator.Current as IDisposable;
                        if (disposable != null)
                        {
                            disposable.Dispose();
                        }
                    }
                }
                this.m_contentByName.Clear();
            }
        }

        public ContentStream PrepareContentStream(ContentEdit.ContentDescription contentDescription)
        {
            contentDescription.Stream.Position = (long)contentDescription.Position;
            return contentDescription.Stream.CreateSubstream(contentDescription.BytesCount);
        }


    }
}