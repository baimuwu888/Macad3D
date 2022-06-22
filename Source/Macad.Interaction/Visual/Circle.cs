﻿using System;
using Macad.Common;
using Macad.Core;
using Macad.Occt;
using Macad.Occt.Extensions;

namespace Macad.Interaction.Visual;

public class Circle: VisualObject
{
    [Flags]
    public enum Style
    {
        None = 0,
        AutoScale = 1 << 0
    }

    //--------------------------------------------------------------------------------------------------

    public override AIS_InteractiveObject AisObject
    {
        get
        {
            _EnsureAisObject();
            return _AisObject;
        }
    }

    //--------------------------------------------------------------------------------------------------

    public Quantity_Color Color
    {
        get { return _Color; }
        set
        {
            _Color = value;
            _UpdatePresentation();
        }
    }
        
    //--------------------------------------------------------------------------------------------------

    public double Radius
    {
        get { return _Radius; }
        set
        {
            _Radius = value;
            _UpdatePresentation();
        }
    }

    //--------------------------------------------------------------------------------------------------
    
    public double Width
    {
        get { return _Width; }
        set
        {
            _Width = value;
            _UpdatePresentation();
        }
    }
    
    //--------------------------------------------------------------------------------------------------
    
    public (double start, double end) Limits
    {
        get { return _Limits; }
        set
        {
            _Limits = value;
            _UpdatePresentation();
        }
    }
        
    //--------------------------------------------------------------------------------------------------
    
    public (double start, double end) Sector
    {
        get { return _Sector; }
        set
        {
            _Sector = value;
            _UpdatePresentation();
        }
    }

    //--------------------------------------------------------------------------------------------------

    public override bool IsSelectable
    {
        get { return _IsSelectable; }
        set
        {
            if (_IsSelectable == value)
                return;

            _IsSelectable = value;
            if (_AisObject != null)
                Update();
        }
    }
    
    //--------------------------------------------------------------------------------------------------

    readonly Style _Style;
    AISX_Circle _AisObject;
    Ax2 _Position = Ax2.XOY;
    double _Radius = 1.0;
    bool _IsSelectable;
    double _Width = 3.0;
    (double start, double end) _Limits;
    (double start, double end) _Sector;
    Quantity_Color _Color = Colors.Auxillary;

    //--------------------------------------------------------------------------------------------------

    public Circle(WorkspaceController workspaceController, Style style)
        : base(workspaceController, null)
    {
        _Style = style;
    }
    
    //--------------------------------------------------------------------------------------------------

    public void Set(gp_Circ circle)
    {
        _Radius = circle.Radius();
        Set(circle.Position());
    }
    
    //--------------------------------------------------------------------------------------------------

    public void Set(Ax2 position)
    {   
        _Position = position;
        if (_AisObject != null)
        {
            _UpdatePresentation();
        }
        else
        {
            Update();
        }
    }

    //--------------------------------------------------------------------------------------------------

    public override void Remove()
    {
        if (_AisObject != null)
        {
            AisContext.Erase(_AisObject, false);
            _AisObject = null;
        }
    }

    //--------------------------------------------------------------------------------------------------

    public override void Update()
    {
        if (_AisObject == null)
        {
            _EnsureAisObject();
        }
        else
        {
            AisContext.Redisplay(_AisObject, false);
        }
        if(_IsSelectable)
            AisContext.Activate(_AisObject);
        else
            AisContext.Deactivate(_AisObject);
    }

    //--------------------------------------------------------------------------------------------------

    void _UpdatePresentation()
    {
        if (_AisObject == null)
            return;

        if (_Style.Has(Style.AutoScale))
        {
            Graphic3d_TransformPers transformPers = new(Graphic3d_TransModeFlags.Graphic3d_TMF_ZoomPers, _Position.Location);
            _AisObject.SetTransformPersistence(transformPers);

            _AisObject.SetLocalTransformation(new Trsf(new Ax3(Pnt.Origin, _Position.Direction, _Position.XDirection), Ax3.XOY));
            double size = 50.0 * WorkspaceController.ActiveViewport.DpiScale;
            _AisObject.SetCircle(new gp_Circ(Ax2.XOY, size));
        }
        else
        {
            _AisObject.SetLocalTransformation(new Trsf(new Ax3(_Position.Location, _Position.Direction, _Position.XDirection), Ax3.XOY));
            _AisObject.SetCircle(new gp_Circ(Ax2.XOY, _Radius));
        }

        _AisObject.SetColor(_Color);
        _AisObject.SetWidth(_Width);
        _AisObject.SetLimits(_Limits.start, _Limits.end);
        _AisObject.SetSector(_Sector.start, _Sector.end);
    }

    //--------------------------------------------------------------------------------------------------
        
    void _EnsureAisObject()
    {
        if (_AisObject != null)
            return;

        _AisObject = new AISX_Circle();

        _UpdatePresentation();

        AisContext.Display(_AisObject, 0, 0, false);
    }

    //--------------------------------------------------------------------------------------------------

}