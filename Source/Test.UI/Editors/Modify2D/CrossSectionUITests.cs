﻿using System.Windows.Forms;
using Macad.Test.UI.Framework;
using NUnit.Framework;

namespace Macad.Test.UI.Editors.Modify2D;

[TestFixture]
public class CrossSectionUITests : UITestBase
{
    [SetUp]
    public void SetUp()
    {
        Reset();
    }

    //--------------------------------------------------------------------------------------------------

    [Test]
    public void Create()
    {
        _CreateCrossSection();

        Assert.AreEqual("CrossSection", Pipe.GetValue<string>("$Selected.Shape.Name"));
    }
    
    //--------------------------------------------------------------------------------------------------

    [Test]
    public void DisableForSketch()
    {
        // Create box
        TestDataGenerator.GenerateSketch(MainWindow);

        // Create imprint on any face
        MainWindow.Ribbon.SelectTab("Model");
        Assert.IsFalse(MainWindow.Ribbon.IsButtonEnabled("CreateCrossSection"));
    }

    //--------------------------------------------------------------------------------------------------

    [Test]
    public void PropertyPanel()
    {
        _CreateCrossSection();

        var panel = MainWindow.PropertyView.FindPanelByClass("CrossSectionPropertyPanel");
        Assert.That(panel, Is.Not.Null);
    }

    //--------------------------------------------------------------------------------------------------

    [Test]
    public void PropPanelFilter()
    {
        _CreateCrossSection();

        var panel = MainWindow.PropertyView.FindPanelByClass("CrossSectionPropertyPanel");
        Assert.That(panel, Is.Not.Null);

        panel.ClickButton("FilterOuter");
        Assert.AreEqual("Outer", Pipe.GetValue("$Selected.Shape.Filter"));

        panel.ClickButton("FilterInner");
        Assert.AreEqual("Inner", Pipe.GetValue("$Selected.Shape.Filter"));

        panel.ClickButton("FilterAll");
        Assert.AreEqual("All", Pipe.GetValue("$Selected.Shape.Filter"));
    }

    //--------------------------------------------------------------------------------------------------

    [Test]
    [TestCase("OrientationY")]
    [TestCase("OrientationP")]
    [TestCase("OrientationR")]
    [TestCase("Offset")]
    public void PropPanelValues(string valueId)
    {
        _CreateCrossSection();

        var panel = MainWindow.PropertyView.FindPanelByClass("CrossSectionPropertyPanel");
        Assert.That(panel, Is.Not.Null);

        Assert.AreEqual(0.0, panel.GetValue<double>(valueId));

        panel.SetValue(valueId, 10.0);

        // Deselect, Reselect
        MainWindow.Viewport.ClickRelative(0.1, 0.1);
        MainWindow.Document.SelectItem("Box_1");
        panel = MainWindow.PropertyView.FindPanelByClass("CrossSectionPropertyPanel");
        Assert.That(panel, Is.Not.Null);
        Assert.AreEqual(10.0, panel.GetValue<double>(valueId));
    }

    //--------------------------------------------------------------------------------------------------

    [Test]
    public void PropPanelTakeWorkingPlane()
    {
        // Create reference object for aligning working plane
        MainWindow.Ribbon.SelectTab("Model");
        MainWindow.Ribbon.ClickButton("CreateCylinder");
        var viewport = MainWindow.Viewport;
        viewport.ClickRelative(0.1, 0.1);
        viewport.ClickRelative(0.3, 0.3);
        viewport.ClickRelative(0.3, 0.25);

        _CreateCrossSection();

        // Select new working plane
        MainWindow.Ribbon.SelectTab("Edit");
        MainWindow.Ribbon.ClickButton("AlignWorkingPlane");
        viewport.ClickRelative(0.1, 0.1);
        var wpln = Pipe.GetValue("$Context.Workspace.WorkingPlane");

        var panel = MainWindow.PropertyView.FindPanelByClass("CrossSectionPropertyPanel");
        Assert.That(panel, Is.Not.Null);
        panel.ClickButton("TakeWorkingPlane");
        Assert.AreNotEqual(wpln, Pipe.GetValue("$Selected.Shape.Plane"));
    }

    //--------------------------------------------------------------------------------------------------
    //--------------------------------------------------------------------------------------------------

    void _CreateCrossSection()
    {
        // Create box
        TestDataGenerator.GenerateBox(MainWindow);

        // Create imprint on any face
        MainWindow.Ribbon.SelectTab("Model");
        Assert.IsTrue(MainWindow.Ribbon.IsButtonEnabled("CreateCrossSection"));
        MainWindow.Ribbon.ClickButton("CreateCrossSection");
    }
}