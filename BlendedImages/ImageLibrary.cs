using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace BlendedImages
{
    public class ImageLibrary
    {
        private List<string> imagePaths;
        private readonly XmlDocument xmlDocument;
        private string libraryPath = "imgLibrary.xml";
        public List<string> ImagePaths
        {
            get { return imagePaths; }
        }

        public ImageLibrary()
        {
            imagePaths = new List<string>();
            xmlDocument = new XmlDocument();
            StreamWriter writer = new StreamWriter(libraryPath);
            xmlDocument.CreateXmlDeclaration("1.0", "UTF-8", "yes");
            xmlDocument.AppendChild(xmlDocument.CreateElement("data"));
            xmlDocument.Save(writer);
            writer.Close();
        }
        public ImageLibrary(XmlDocument document)
        {
            xmlDocument = document;
            imagePaths = new List<string>();
            XmlNodeList list = document.DocumentElement.SelectNodes("/data/Image");
            if(list == null)
                return;
            foreach (XmlElement node in list)
            {
                imagePaths.Add(node.GetAttribute("path"));
            }
        }

        public bool Add(string path)
        {
            if (imagePaths.Contains(path))
                return false;
            imagePaths.Add(path);
            XmlElement element = xmlDocument.CreateElement("Image");
            element.SetAttribute("path", path);
            xmlDocument.DocumentElement.AppendChild(element);
            SaveXML();
            return true;

        }

        private void SaveXML()
        {
            StreamWriter writer;
            try
            {
                writer = new StreamWriter(libraryPath);
                xmlDocument.Save(writer);
                writer.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            writer.Close();
        }

        public bool Delete(string path)
        {
            if (!imagePaths.Remove(path))
                return false;
            XmlElement element = null;
            XmlNodeList list = xmlDocument.DocumentElement.SelectNodes("/data/Image");
            if (list == null)
                return false;
            foreach (XmlElement node in list)
            {
                if (node.GetAttribute("path") == path)
                {
                    element = node;
                    break;
                }
            }
            if (element == null)
                return false;
            xmlDocument.DocumentElement.RemoveChild(element);
            SaveXML();
            return true;
        }
    
    }
}