using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Mime;
using System.Xml.Linq;
using PressSharper;

namespace VmixerTools
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var receipts = ReceiptsParser.ParseReceipts("aromas.xml","receipts.xml","images.xml");
         
            Console.ReadKey();
        }
    }

    public static class ReceiptsParser
    {
        public static IEnumerable<Receipt> ParseReceipts(string aromasXml, string receiptsXml, string imageLinksXml)
        {
            var aroms = ParseAromas(aromasXml).ToArray();
            var imageLinks = ParseImageLinks(imageLinksXml).ToArray();
            var receipts = ParseReceipts(receiptsXml).ToArray();
            foreach (var receipt in receipts)
            {
                foreach (var aroma in receipt.Aromas)
                {
                    aroma.Key.Name = aroms.FirstOrDefault(b => b.Id == aroma.Key.Id)?.Name;
                }

                receipt.ImageName = imageLinks.FirstOrDefault(l => l.ReceiptId == receipt.Id)?.Name;
            }

            return receipts;
        }
        
        private static IEnumerable<ImageLink> ParseImageLinks(string source)
        {
            var document = XDocument.Load(source);
            var blog = new Blog(document);
            return blog.GetImageLinks();
        }

        private static IEnumerable<Aroma> ParseAromas(string source)
        {
            var document = XDocument.Load(source);
            var blog = new Blog(document);
            return blog.GetAromas();
        }

        private static IEnumerable<Receipt> ParseReceipts(string source)
        {
            var document = XDocument.Load(source);
            var blog = new Blog(document);
            var receipts = blog.GetReceipts();
            return receipts.Where(r => r != null);
        }
    }
}