﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Macad.Common.Serialization;
using Macad.Occt;

namespace Macad.Core.Shapes
{
    [SerializeType]
    public sealed class Sketch : Shape2D
    {
        #region Subtypes

        [Flags]
        public enum ElementType
        {
            Point = 1,
            Segment = 2,
            Constraint = 4
        }

        //--------------------------------------------------------------------------------------------------

        #endregion

        #region Properties
        
        //--------------------------------------------------------------------------------------------------

        public override ShapeType ShapeType
        {
            get { return ShapeType.Sketch; }
        }

        //--------------------------------------------------------------------------------------------------

        [SerializeMember]
        public Dictionary<int, SketchSegment> Segments
        {
            get { return _Segments; }
            set
            {
                if (_Segments != value)
                {
                    SaveUndo(ElementType.Segment);
                    _Segments = value;
                    Invalidate();
                    RaisePropertyChanged();
                }
            }
        }

        //--------------------------------------------------------------------------------------------------

        [SerializeMember]
        public Dictionary<int,Pnt2d> Points
        {
            get { return _Points; }
            set
            {
                if (_Points != value)
                {
                    SaveUndo(ElementType.Point);
                    _Points = value;
                    Invalidate();
                    RaisePropertyChanged();
                }
            }
        }

        //--------------------------------------------------------------------------------------------------

        [SerializeMember]
        public List<SketchConstraint> Constraints
        {
            get { return _Constraints; }
            set
            {
                if (_Constraints != value)
                {
                    SaveUndo(ElementType.Constraint);
                    _Constraints = value;
                    Invalidate();
                    RaisePropertyChanged();
                }
            }
        }

        //--------------------------------------------------------------------------------------------------

        public bool ConstraintSolverFailed
        {
            get { return _ConstraintSolverFailed; }
            set
            {
                if (_ConstraintSolverFailed != value)
                {
                    _ConstraintSolverFailed = value;
                    RaisePropertyChanged();
                }
            }
        }

        //--------------------------------------------------------------------------------------------------

        #endregion

        #region Initialization

        public static Sketch Create()
        {
            return new Sketch();
        }

        #endregion

        #region Members

        Dictionary<int, SketchSegment> _Segments = new Dictionary<int, SketchSegment>();
        Dictionary<int, Pnt2d> _Points = new Dictionary<int, Pnt2d>();
        List<SketchConstraint> _Constraints = new List<SketchConstraint>();
        bool _ConstraintSolverFailed;

        //--------------------------------------------------------------------------------------------------

        #endregion

        #region Elements

        public int AddPoint(Pnt2d point)
        {
            var index = Points.Keys.Any() ? Points.Keys.Max() + 1 : 0;
            Points.Add(index, point);
            return index;
        }

        //--------------------------------------------------------------------------------------------------

        public void MergePoints(int replace, int with)
        {
            Debug.Assert(with != replace);
            SaveUndo(ElementType.Point);
            SaveUndo(ElementType.Segment);

            Points[with] = Points[replace];

            bool changedSegments = false;
            foreach (var segment in Segments.Values)
            {
                for (var i = 0; i < segment.Points.Length; i++)
                {
                    if (!changedSegments)
                    {
                        SaveUndo(ElementType.Segment);
                        changedSegments = true;
                    }
                    if (segment.Points[i] == replace)
                        segment.Points[i] = with;
                }
            }

            bool changedConstraints = false;
            foreach (var constraint in Constraints)
            {
                if (constraint.Points == null)
                    continue;

                for (var i = 0; i < constraint.Points.Length; i++)
                {
                    if (!changedConstraints)
                    {
                        SaveUndo(ElementType.Constraint);
                        changedConstraints = true;
                    }
                    if (constraint.Points[i] == replace)
                        constraint.Points[i] = with;
                }
            }

            Points.Remove(replace);

            Invalidate();

            RaisePropertyChanged("Points");
            if (changedSegments)
                RaisePropertyChanged("Segments");
            if (changedConstraints)
                RaisePropertyChanged("Constraints");

            ElementType types = ElementType.Point;
            if (changedConstraints) types |= ElementType.Constraint;
            if (changedSegments) types |= ElementType.Segment;
            OnElementsChanged(types);
        }

        //--------------------------------------------------------------------------------------------------

        public bool SetPoint(int index, Pnt2d pnt2d)
        {
            if (!Points.ContainsKey(index))
                return false;

            SaveUndo(ElementType.Point);
            Points[index] = pnt2d;

            RaisePropertyChanged("Points");

            OnElementsChanged(ElementType.Point);
            return true;
        }

        //--------------------------------------------------------------------------------------------------

        public void DeletePoint(int point)
        {
            SaveUndo(ElementType.Point);

            // Look out for segments who reference this point
            bool changedSegments = false;
            var segs = Segments.ToArray();
            var removedSegs = new List<int>();
            foreach (var segmentKvp in segs)
            {
                for (var i = 0; i < segmentKvp.Value.Points.Length; i++)
                {
                    if (segmentKvp.Value.Points[i] == point)
                    {
                        if (!changedSegments)
                        {
                            SaveUndo(ElementType.Segment);
                            changedSegments = true;
                        }
                        removedSegs.Add(segmentKvp.Key);
                        _Segments.Remove(segmentKvp.Key);
                    }
                }
            }
            _Points.Remove(point);

            var deletedPoints = DeleteOrphanedPoints(false);
            deletedPoints.Add(point);

            // Look out for constraints who reference this point
            bool changedConstraints = false;
            var cons = Constraints.ToArray();
            foreach (var constraint in cons)
            {
                if (constraint.Points != null)
                {
                    for (var i = 0; i < constraint.Points.Length; i++)
                    {
                        if (deletedPoints.Contains(constraint.Points[i]))
                        {
                            if (!changedConstraints)
                            {
                                SaveUndo(ElementType.Constraint);
                                changedConstraints = true;
                            }
                            _Constraints.Remove(constraint);
                        }
                    }
                }
                if (constraint.Segments != null)
                {
                    for (var i = 0; i < constraint.Segments.Length; i++)
                    {
                        if (removedSegs.Contains(constraint.Segments[i]))
                        {
                            if (!changedConstraints)
                            {
                                SaveUndo(ElementType.Constraint);
                                changedConstraints = true;
                            }
                            _Constraints.Remove(constraint);
                        }
                    }
                }
            }

            if (changedSegments)
                RaisePropertyChanged("Segments");
            if (changedConstraints)
                RaisePropertyChanged("Constraints");
            RaisePropertyChanged("Points");

            ElementType types = ElementType.Point;
            if (changedConstraints) types |= ElementType.Constraint;
            if (changedSegments) types |= ElementType.Segment;
            OnElementsChanged(types);
        }

        //--------------------------------------------------------------------------------------------------

        public int AddSegment(SketchSegment segment)
        {
            var index = Segments.Keys.Any() ? Segments.Keys.Max() + 1 : 0;
            Segments.Add(index, segment);
            return index;
        }

        //--------------------------------------------------------------------------------------------------

        public int AddConstraint(SketchConstraint constraint)
        {
            Constraints.Add(constraint);
            return Constraints.Count - 1;
        }

        //--------------------------------------------------------------------------------------------------

        public bool SetConstraintParameter(SketchConstraint constraint, double parameter)
        {
            if (!Constraints.Contains(constraint))
                return false;

            SaveUndo(ElementType.Constraint);
            constraint.Parameter = parameter;

            if(SolveConstraints(false))
                SolveConstraints(true);

            RaisePropertyChanged("Constraints");

            OnElementsChanged(ElementType.Constraint);
            return true;
        }

        //--------------------------------------------------------------------------------------------------

        public void DeleteConstraint(SketchConstraint conToDelete)
        {
            SaveUndo(ElementType.Constraint);

            // Remove constraint
            _Constraints.Remove(conToDelete);

            RaisePropertyChanged("Constraints");

            OnElementsChanged(ElementType.Constraint);
        }

        //--------------------------------------------------------------------------------------------------

        public (IDictionary<int, int> pointMap, IDictionary<int, int> segmentMap, IEnumerable<int> constraints) 
            AddElements(IDictionary<int, Pnt2d> points, int[] mergePointIndices, IDictionary<int, SketchSegment> segments, IEnumerable<SketchConstraint> constraints)
        {
            Sketch.ElementType changedTypes = 0;
            Dictionary<int, int> pointMap = null;
            Dictionary<int, int> segmentMap = null;

            if (points != null)
            {
                pointMap = new Dictionary<int, int>(points.Count);

                SaveUndo(ElementType.Point);
                changedTypes |= ElementType.Point;

                foreach (var pointKvp in points)
                {
                    pointMap.Add(pointKvp.Key, (mergePointIndices != null)&&(mergePointIndices[pointKvp.Key] >= 0) ? mergePointIndices[pointKvp.Key] : AddPoint(pointKvp.Value));
                }

                if (segments != null)
                {
                    segmentMap = new Dictionary<int, int>();

                    SaveUndo(ElementType.Segment);
                    changedTypes |= ElementType.Segment;

                    foreach (var segmentKvp in segments)
                    {
                        var segment = segmentKvp.Value;
                        for (int i = 0; i < segment.Points.Length; i++)
                        {
                            segment.Points[i] = pointMap[segment.Points[i]];
                        }
                        segmentMap.Add( segmentKvp.Key, AddSegment(segment) );
                    }
                }
            }

            List<int> newConstraints = default;
            if (constraints != null)
            {
                SaveUndo(ElementType.Constraint);
                changedTypes |= ElementType.Constraint;
                SaveUndo(ElementType.Point);
                changedTypes |= ElementType.Point;

                newConstraints = new List<int>();

                foreach (var constraint in constraints)
                {
                    if ((pointMap != null) && (constraint.Points != null))
                    {
                        for (int i = 0; i < constraint.Points.Length; i++)
                        {
                            constraint.Points[i] = pointMap[constraint.Points[i]];
                        }
                    }
                    if ((segmentMap != null) && (constraint.Segments != null))
                    {
                        for (int i = 0; i < constraint.Segments.Length; i++)
                        {
                            constraint.Segments[i] = segmentMap[constraint.Segments[i]];
                        }
                    }

                    newConstraints.Add(AddConstraint(constraint));
                }
            }

            if (changedTypes != 0)
                OnElementsChanged(changedTypes);

            Invalidate();

            return (pointMap, segmentMap, newConstraints);
        }

        //--------------------------------------------------------------------------------------------------

        public void DeleteSegment(SketchSegment segToDelete)
        {
            SaveUndo(ElementType.Point);
            SaveUndo(ElementType.Segment);
            int segIdToDelete = _Segments.First(kvp => kvp.Value == segToDelete).Key;

            // Look out for constraints who reference this segment
            bool changedConstraints = false;
            var cons = Constraints.ToArray();
            foreach (var constraint in cons)
            {
                if (constraint.Segments == null)
                    continue;

                for (var i = 0; i < constraint.Segments.Length; i++)
                {
                    if (constraint.Segments[i] == segIdToDelete)
                    {
                        if (!changedConstraints)
                        {
                            SaveUndo(ElementType.Constraint);
                            changedConstraints = true;
                        }
                        _Constraints.Remove(constraint);
                    }
                }
            }

            // Remove segment
            _Segments.Remove(segIdToDelete);

            if (changedConstraints)
                RaisePropertyChanged("Constraints");
            RaisePropertyChanged("Segments");
            
            var deletedPoints = DeleteOrphanedPoints();

            // Look out for constraints who reference this segment
            foreach (var constraint in cons)
            {
                if (constraint.Points == null)
                    continue;

                for (var i = 0; i < constraint.Points.Length; i++)
                {
                    if (deletedPoints.Contains(constraint.Points[i]))
                    {
                        if (!changedConstraints)
                        {
                            SaveUndo(ElementType.Constraint);
                            changedConstraints = true;
                        }
                        _Constraints.Remove(constraint);
                    }
                }
            }

            ElementType types = ElementType.Segment | ElementType.Point;
            if (changedConstraints) types |= ElementType.Constraint;
            
            OnElementsChanged(types);

            Invalidate();
        }

        //--------------------------------------------------------------------------------------------------

        List<int> DeleteOrphanedPoints(bool doPropertyChangeCalls = true)
        {
            // Check which points are without any segment
            var pointsToDelete = new List<int>(_Points.Keys);
            foreach (var segment in _Segments.Values)
            {
                pointsToDelete.RemoveAll(pointIndex => segment.Points.Contains(pointIndex));
                if (!pointsToDelete.Any())
                    break;
            }

            if (pointsToDelete.Any())
            {
                if (doPropertyChangeCalls)
                    SaveUndo(ElementType.Point);
                foreach (var pointIndex in pointsToDelete)
                {
                    _Points.Remove(pointIndex);
                }

                if (doPropertyChangeCalls)
                    RaisePropertyChanged("Points");
            }

            return pointsToDelete;
        }

        //--------------------------------------------------------------------------------------------------

        public void Clear()
        {
            SaveUndo(ElementType.Point);
            SaveUndo(ElementType.Segment);
            SaveUndo(ElementType.Constraint);

            var points = _Points.Keys.ToArray();
            var segments = _Segments.Keys.ToArray();

            _Constraints.Clear();
            foreach (var segIndex in segments)
            {
                _Segments.Remove(segIndex);
            }
            foreach (var pointIndex in points)
            {
                _Points.Remove(pointIndex);
            }
            
            OnElementsChanged(ElementType.Segment | ElementType.Point | ElementType.Constraint);

            Invalidate();
        }

        //--------------------------------------------------------------------------------------------------

        public delegate void SketchElementChange(Sketch sketch, ElementType types);

        public event SketchElementChange ElementsChanged;

        public void OnElementsChanged(ElementType types)
        {
            if (ElementsChanged != null) ElementsChanged(this, types);
        }

        //--------------------------------------------------------------------------------------------------

        #endregion

        #region Undo

        public void SaveUndo(ElementType type)
        {
            switch (type)
            {
                case ElementType.Point:
                    SaveUndo("Points", new Dictionary<int,Pnt2d>(_Points));
                    break;

                case ElementType.Segment:
                    var newDict = new Dictionary<int, SketchSegment>(_Segments.Count);
                    foreach (var segmentKvp in Segments)
                    {
                        newDict.Add(segmentKvp.Key, segmentKvp.Value.Clone());
                    }
                    SaveUndo("Segments", newDict);
                    break;

                case ElementType.Constraint:
                    SaveUndo("Constraints", _Constraints.ConvertAll(con => con.Clone()));
                    break;
            }
        }

        //--------------------------------------------------------------------------------------------------

        public override void OnAfterUndo()
        {
            foreach (var kvp in _Segments)
            {
                kvp.Value.Invalidate();
            }

            OnElementsChanged(ElementType.Point | ElementType.Segment | ElementType.Constraint);

            base.OnAfterUndo();
        }

        //--------------------------------------------------------------------------------------------------

        #endregion

        #region Make

        public bool SolveConstraints(bool precise)
        {
            if (Constraints.Count == 0)
            {
                ConstraintSolverFailed = false;
                return true;
            }

            if (SketchConstraintSolver.Solve(this, precise))
            {
                Invalidate();
                RaisePropertyChanged("Points");
                OnElementsChanged(ElementType.Point);
                if (precise)
                {
                    ConstraintSolverFailed = false;
                }
                return true;
            }
            if (precise)
            {
                Messages.Error("Sketch constraints failed to solve.");
                ConstraintSolverFailed = true;
            }
            return false;
        }

        //--------------------------------------------------------------------------------------------------

        protected override bool MakeInternal(MakeFlags flags)
        {
            if (!Segments.Any() || !Points.Any())
            {
                var makeVertex = new BRepBuilderAPI_MakeVertex(Pnt.Origin);
                BRep = makeVertex.Vertex();
                HasErrors = false;
                return base.MakeInternal(flags);
            }

            // Create edges
            var freeSegmentEdges = new Dictionary<SketchSegment, TopoDS_Edge>();
            foreach (var segmentKvp in _Segments)
            {
                var segment = segmentKvp.Value;
                if(segment.IsAuxilliary)
                    continue;

                var segEdge = segment.MakeEdge(_Points);
                if (segEdge == null)
                {
                    Messages.Warning($"The segment {segmentKvp.Key} of type {segment.GetType().Name} failed creating an edge.");
                    continue;
                }
                freeSegmentEdges.Add(segment, segEdge);
                AddNamedSubshape("seg", segEdge, segmentKvp.Key);
            }

            // Create wires
            var wires = new List<TopoDS_Wire>();
            while (freeSegmentEdges.Any())
            {
                var nextSegmentEdge = freeSegmentEdges.First();
                var firstSegmentOfWire = nextSegmentEdge.Key;
                freeSegmentEdges.Remove(nextSegmentEdge.Key);

                var makeWire = new BRepBuilderAPI_MakeWire(nextSegmentEdge.Value);

                if ((firstSegmentOfWire.StartPoint != -1) || (firstSegmentOfWire.EndPoint != -1))
                {
                    while (freeSegmentEdges.Any())
                    {
                        nextSegmentEdge = freeSegmentEdges.FirstOrDefault(kvp => kvp.Key.IsConnected(nextSegmentEdge.Key));
                        if (nextSegmentEdge.Value == null)
                        {
                            // disconnected segment
                            break;
                        }

                        makeWire.Add(nextSegmentEdge.Value);
                        freeSegmentEdges.Remove(nextSegmentEdge.Key);
                    }
                }

                // Get wire shape
                var wire = makeWire.Wire();
                if (wire == null)
                {
                    Messages.Error("Error when creating a wire.");
                    return false;
                }

                wires.Add(wire);
            }

            // Create resulting shape
            var builder = new TopoDS_Builder();
            var shape = new TopoDS_Compound();
            builder.MakeCompound(shape);

            foreach (var wire in wires)
            {
                builder.Add(shape, wire);
            }

            BRep = shape;

            return base.MakeInternal(flags);
        }

        //--------------------------------------------------------------------------------------------------

        #endregion

    }
}