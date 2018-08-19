using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using PressSharper;

namespace VmixerTools
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var receiptBuilder = new ReceiptBuilder();

            var document = XDocument.Load("wordpress.2018-08-18.xml");
            var blog = new Blog(document);
            var posts = blog.
        }
    }

    public class ReceiptBuilder
    {
        public Receipt Build(XElement receipt)
        {
            var splitedTitle = receipt.Element("title").Value.Split('\"');
            var description = receipt
                .Elements("postmeta")
                .Where(m => m.Element("meta_key").Value == "<![CDATA[preview_text]]>")
                .Select(m=>m.Element("meta_value").Value).FirstOrDefault();
            return new Receipt
            {
                Name = splitedTitle[1],
                Manufacturer = splitedTitle[3],
                Description = description
            };
        }
    }

    public static class PressSharperpExtensions
    {
        
    }
}