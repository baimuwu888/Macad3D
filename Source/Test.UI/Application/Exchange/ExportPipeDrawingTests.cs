﻿using System.IO;
using Macad.Test.UI.Framework;
using NUnit.Framework;

namespace Macad.Test.UI.Application.Exchange
{
    [TestFixture]
    public class ExportPipeDrawingTests : UITestBase
    {
        [SetUp]
        public void SetUp()
        {
            Reset();
        }

        //--------------------------------------------------------------------------------------------------

        [Test]
        public void DisableWithoutPipeModifier()
        {
            TestDataGenerator.GenerateBox(MainWindow);
            
            MainWindow.Ribbon.SelectTab("Toolbox");
            Assert.IsFalse(MainWindow.Ribbon.IsButtonEnabled("ExportPipeDrawing"));
        }
        
        //--------------------------------------------------------------------------------------------------

        [Test]
        public void Export()
        {
            var path = Path.Combine(FileDialogAdaptor.GetTempPath(), "pipe.svg");

            TestDataGenerator.GenerateSketch(MainWindow);
            MainWindow.Ribbon.ClickButton("CloseSketchEditor");

            MainWindow.Ribbon.SelectTab("Model");
            Assume.That(MainWindow.Ribbon.IsButtonEnabled("CreatePipe"));
            MainWindow.Ribbon.ClickButton("CreatePipe");
            
            MainWindow.Ribbon.SelectTab("Toolbox");
            Assert.IsTrue(MainWindow.Ribbon.IsButtonEnabled("ExportPipeDrawing"));
            MainWindow.Ribbon.ClickButton("ExportPipeDrawing");

            var fileDlg = new FileDialogAdaptor(MainWindow);
            fileDlg.SelectFileType("*.svg");
            fileDlg.Save(path, checkFile:false);
            Assert.IsFalse(FileDialogAdaptor.IsDialogOpen(MainWindow));
            Assert.IsFalse(WindowAdaptor.IsWindowOpen(MainWindow, "ExportViewportHlr"));

            var dlg = new WindowAdaptor(MainWindow, "ExchangerSettings");
            dlg.ClickButton("Ok");
            Assert.IsFalse(WindowAdaptor.IsWindowOpen(MainWindow, "ExchangerSettings"));

            Assert.IsTrue(System.IO.File.Exists(path));

        }

        //--------------------------------------------------------------------------------------------------

        [Test]
        public void ExportCancelSettings()
        {
            var path = Path.Combine(FileDialogAdaptor.GetTempPath(), "pipe.svg");

            TestDataGenerator.GenerateSketch(MainWindow);
            MainWindow.Ribbon.ClickButton("CloseSketchEditor");

            MainWindow.Ribbon.SelectTab("Model");
            Assume.That(MainWindow.Ribbon.IsButtonEnabled("CreatePipe"));
            MainWindow.Ribbon.ClickButton("CreatePipe");
            
            MainWindow.Ribbon.SelectTab("Toolbox");
            Assert.IsTrue(MainWindow.Ribbon.IsButtonEnabled("ExportPipeDrawing"));
            MainWindow.Ribbon.ClickButton("ExportPipeDrawing");

            var fileDlg = new FileDialogAdaptor(MainWindow);
            fileDlg.SelectFileType("*.svg");
            fileDlg.Save(path, checkFile:false);
            Assert.IsFalse(FileDialogAdaptor.IsDialogOpen(MainWindow));
            Assert.IsFalse(WindowAdaptor.IsWindowOpen(MainWindow, "ExportViewportHlr"));

            var dlg = new WindowAdaptor(MainWindow, "ExchangerSettings");
            dlg.ClickButton("Cancel");
            Assert.IsFalse(WindowAdaptor.IsWindowOpen(MainWindow, "ExchangerSettings"));

            Assert.IsFalse(System.IO.File.Exists(path));

        }

        //--------------------------------------------------------------------------------------------------

        [Test]
        public void ExportCancelFileDlg()
        {
            TestDataGenerator.GenerateSketch(MainWindow);
            MainWindow.Ribbon.ClickButton("CloseSketchEditor");

            MainWindow.Ribbon.SelectTab("Model");
            Assume.That(MainWindow.Ribbon.IsButtonEnabled("CreatePipe"));
            MainWindow.Ribbon.ClickButton("CreatePipe");
            
            MainWindow.Ribbon.SelectTab("Toolbox");
            Assert.IsTrue(MainWindow.Ribbon.IsButtonEnabled("ExportPipeDrawing"));
            MainWindow.Ribbon.ClickButton("ExportPipeDrawing");

            var fileDlg = new FileDialogAdaptor(MainWindow);
            fileDlg.ClickButton(FileDialogAdaptor.Button.Cancel);
            Assert.IsFalse(FileDialogAdaptor.IsDialogOpen(MainWindow));
        }

        //--------------------------------------------------------------------------------------------------

    }
}