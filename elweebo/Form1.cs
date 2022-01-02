using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WanaKanaSharp;
using Kawazu;

namespace elweebo
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            openFileDialog2 = new OpenFileDialog()
            {
                FileName = "Select a text file",
                Filter = "epub files (*.epub)|*.epub",
                Title = "Open text file"
            };

            selectButton = new Button()
            {
                Size = new Size(100, 20),
                Location = new Point(15, 15),
                Text = "Select file"
            };
            selectButton.Click += new EventHandler(selectButton_Click);
            Controls.Add(selectButton);

        }

        private Button selectButton;
        private OpenFileDialog openFileDialog2;

    


        private async void selectButton_Click(object sender, EventArgs e)
        {
            if (openFileDialog2.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var filePath = openFileDialog2.FileName;
                    var currentpath = Path.GetDirectoryName(filePath);
                    Directory.SetCurrentDirectory(currentpath);
                    ZipFile.ExtractToDirectory(filePath, "./extracted");
                    Copy("./extracted", "./modified");
                    var removes1 = Directory.GetFiles("./modified","*.html",SearchOption.AllDirectories);
                    var removes2 = Directory.GetFiles("./modified", "*.xhtml", SearchOption.AllDirectories);
                    var filesToRemove = removes1.Union(removes2).ToArray();
                    foreach(var file in filesToRemove)
                    {
                        File.Delete(file);
                    }
                    Directory.SetCurrentDirectory("./extracted");
                    var files1 = Directory.GetFiles("./", "*.html", SearchOption.AllDirectories);
                    var files2= Directory.GetFiles("./", "*.xhtml", SearchOption.AllDirectories);
                    var filesToModify = files1.Union(files2).ToArray();
                    var converter = new KawazuConverter();
                    foreach(var file in filesToModify)
                    {
                        var writeLocation = Path.Combine("../modified", file);
                        var modifiedStream = File.Create(writeLocation);
                        using (var readStream = new StreamReader(file))
                        {
                            using (var writeStream = new StreamWriter(modifiedStream))
                            {

                                while (readStream.Peek() >= 0)
                                {
                                    var ch = readStream.ReadLine();
                                    string output = "";
                                    var divs = await converter.GetDivisions(ch);
                                    //divides text into proper japanese reading segments, does whatever with non japanese text
                                    //breaks spacing if non japanese is there, which means it breaks spacing on html and needs fixing which i am hacking around cause lazy
                                    foreach (var div in divs)
                                    {
                                        string fulldiv = "";
                                        foreach (var idiom in div)
                                        {
                                            fulldiv += idiom.Element;
                                        }
                                        //ugly spacing hack fix, but this whole thing is ugly hackery so meh
                                        var spaceafter = ch.Contains(fulldiv + " ");
                                        bool iskanji = false;
                                        // IsKanji check fails if there is a single non kanji character, breaking words like 目指す
                                        // check if a any characters in an idiom are a kanji so we we can resolve the furigana 
                                        foreach (char chr in fulldiv)
                                        {
                                            if (WanaKana.IsKanji(chr.ToString()))
                                            {
                                                iskanji = true;
                                                break;
                                            }
                                        }
                                        if (iskanji)
                                        {
                                            output += await converter.Convert(fulldiv, To.Hiragana, Mode.Furigana);
                                        }
                                        else
                                        {
                                            output += fulldiv;
                                            //if (spaceafter) output += " ";
                                        }
                                    }
                                    
                                    writeStream.WriteLine(output);
                                }

                            }
                        }
                    }
                    Directory.SetCurrentDirectory("../");
                    var filename = "./" + Path.GetFileNameWithoutExtension(filePath) + "_modified.epub";
                    ZipFile.CreateFromDirectory("./modified", filename);
                    Directory.Delete("./modified",true);
                    Directory.Delete("./extracted", true);
                    MessageBox.Show($"done!\nyou can find the new file next to the old one with a _modified suffix.");

                }
                catch (SecurityException ex)
                {
                    MessageBox.Show($"Security error.\n\nError message: {ex.Message}\n\n" +
                    $"Details:\n\n{ex.StackTrace}");
                }
            }
        }

        public static void Copy(string sourceDirectory, string targetDirectory)
        {
            var diSource = new DirectoryInfo(sourceDirectory);
            var diTarget = new DirectoryInfo(targetDirectory);

            CopyAll(diSource, diTarget);
        }

        public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }
    }
}
