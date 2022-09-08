using BuildTaskVersionControl;
using Microsoft.Build.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable CS0436 // Typenkonflikte mit importiertem Typ

namespace Test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //File.WriteAllText("input.txt", "[assembly: AssemblyVersion(\"1.2.3\")]\r\n[assembly: AssemblyFileVersion(\"1.2.3\")]");
            //File.WriteAllText("output.txt", "[assembly: AssemblyVersion(\"1.0.0\")]\r\n[assembly: AssemblyFileVersion(\"1.0.0\")]");

            var vt = new VersioningTask();
            vt.InputFile = "input.txt";
            vt.UpdateFiles = new ITaskItem[] { new Item("input.txt"), new Item("output.txt") };
            vt.AutoIncrease = true;
            vt.MaxMatch = 1;
            vt.Execute();
        }
    }

    public class Item : ITaskItem
    {
        public Item(string itemSpec)
        {
            this.ItemSpec = itemSpec;
        }

        public string ItemSpec { get; set; }

        public ICollection MetadataNames => Array.Empty<string>();

        public int MetadataCount => 0;

        public IDictionary CloneCustomMetadata()
        {
            return null;
        }

        public void CopyMetadataTo(ITaskItem destinationItem)
        {
        }

        public string GetMetadata(string metadataName)
        {
            return "";
        }

        public void RemoveMetadata(string metadataName)
        {
        }

        public void SetMetadata(string metadataName, string metadataValue)
        {
        }
    }
}
