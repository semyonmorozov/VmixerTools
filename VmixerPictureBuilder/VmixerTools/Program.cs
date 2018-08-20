using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using PressSharper;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading.Tasks;

namespace VmixerTools
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var receipts = ReceiptsParser.ParseReceipts("aromas.xml", "receipts.xml", "images.xml")
                .Where(i => i != null).ToArray();
            DrawReceipts(receipts).Wait();
        }

        private static async Task DrawReceipts(IEnumerable<Receipt> receipts)
        {
            foreach (var receipt in receipts)
            {
                await DrawReceipt(receipt).ConfigureAwait(false);
            }
        }

        private static async Task DrawReceipt(Receipt receipt)
        {
            await Task.Run(() =>
            {
                var imgPath = receipt.ImagePath;
                Image bitMapImage;
                try
                {
                    bitMapImage = Image.FromFile(imgPath);
                }
                catch (FileNotFoundException e)
                {
                    Console.WriteLine(e);
                    return;
                }

                var graphicImage = Graphics.FromImage(bitMapImage);
                graphicImage.SmoothingMode = SmoothingMode.AntiAlias;
                graphicImage.DrawString("That's my boy!",
                    new Font("Arial", 12, FontStyle.Bold),
                    SystemBrushes.WindowText, new Point(100, 250));
                bitMapImage.Save($"receiptImages/{imgPath.Split('/').Last()}");
            }).ConfigureAwait(false);
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

                receipt.ImagePath = imageLinks.FirstOrDefault(l => l.ReceiptId == receipt.Id)?.Name;
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