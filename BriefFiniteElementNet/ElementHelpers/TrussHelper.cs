﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BriefFiniteElementNet.Elements;
using BriefFiniteElementNet.Integration;
using BriefFiniteElementNet.Loads;

namespace BriefFiniteElementNet.ElementHelpers
{
    /// <summary>
    /// Represents an element helper for truss element.
    /// </summary>
    public class TrussHelper : IElementHelper
    {
        public Element TargetElement { get; set; }

        /// <inheritdoc/>
        public Matrix GetBMatrixAt(Element targetElement, params double[] isoCoords)
        {
            var elm = targetElement as BarElement;

            if (elm == null)
                throw new Exception();

            var l = (elm.EndNode.Location - elm.StartNode.Location).Length;

            var buf = new Matrix(1, 2);

            buf.FillRow(0, -1 / l, 1 / l);

            return buf;
        }

        /// <inheritdoc/>
        public Matrix GetB_iMatrixAt(Element targetElement, int i, params double[] isoCoords)
        {
            if (i != 0)
                throw new Exception();

            var elm = targetElement as BarElement;

            if (elm == null)
                throw new Exception();

            var l = (elm.EndNode.Location - elm.StartNode.Location).Length;

            var buf = new Matrix(1, 2);

            buf.FillRow(0, -1 / l, 1 / l);


            return buf;
        }

        /// <inheritdoc/>
        public Matrix GetDMatrixAt(Element targetElement, params double[] isoCoords)
        {
            var elm = targetElement as BarElement;

            if (elm == null)
                throw new Exception();

            var xi = isoCoords[0];

            var geo = elm.Section.GetCrossSectionPropertiesAt(xi);
            var mech = elm.Material.GetMaterialPropertiesAt(xi);

            var buf = new Matrix(1, 1);

            buf.FillRow(0, geo.A*mech.Ex);

            return buf;
        }

        /// <inheritdoc/>
        public Matrix GetRhoMatrixAt(Element targetElement, params double[] isoCoords)
        {
            var elm = targetElement as BarElement;

            if (elm == null)
                throw new Exception();

            var xi = isoCoords[0];

            var geo = elm.Section.GetCrossSectionPropertiesAt(xi);
            var mech = elm.Material.GetMaterialPropertiesAt(xi);

            var buf = new Matrix(1, 1);

            buf[0, 0] = geo.A * mech.Rho;

            return buf;
        }

        /// <inheritdoc/>
        public Matrix GetMuMatrixAt(Element targetElement, params double[] isoCoords)
        {
            var elm = targetElement as BarElement;

            if (elm == null)
                throw new Exception();

            var xi = isoCoords[0];

            var geo = elm.Section.GetCrossSectionPropertiesAt(xi);
            var mech = elm.Material.GetMaterialPropertiesAt(xi);

            var buf = new Matrix(1, 1);

            buf[0, 0] = geo.A * mech.Mu;

            return buf;
        }

        /// <inheritdoc/>
        public Matrix GetNMatrixAt(Element targetElement, params double[] isoCoords)
        {
            var xi = isoCoords[0];

            if (xi < -1 || xi > 1)
                throw new ArgumentOutOfRangeException(nameof(isoCoords));

            var bar = targetElement as BarElement;

            if (bar == null)
                throw new Exception();

            var n1 = 1 / 2.0 - xi / 2;
            var n2 = 1 / 2.0 + xi / 2;

            var buf = new Matrix(1, 2);

            double[] arr;

            arr = new double[] { n1, n2 };
            
            buf.FillRow(0, arr);

            return buf;
        }

        /// <inheritdoc/>
        public Matrix GetJMatrixAt(Element targetElement, params double[] isoCoords)
        {
            var bar = targetElement as BarElement;

            if (bar == null)
                throw new Exception();

            var buf = new Matrix(1, 1);

            buf[0, 0] = (bar.EndNode.Location - bar.StartNode.Location).Length / 2;

            return buf;
        }

        /// <inheritdoc/>
        public Matrix CalcLocalKMatrix(Element targetElement)
        {
            var buf = ElementHelperExtensions.CalcLocalKMatrix_Bar(this, targetElement);

            return buf;
        }

        /// <inheritdoc/>
        public Matrix CalcLocalMMatrix(Element targetElement)
        {
            var buf = ElementHelperExtensions.CalcLocalMMatrix_Bar(this, targetElement);

            return buf;
        }

        /// <inheritdoc/>
        public Matrix CalcLocalCMatrix(Element targetElement)
        {
            return ElementHelperExtensions.CalcLocalCMatrix_Bar(this, targetElement);
        }

        /// <inheritdoc/>
        public FluentElementPermuteManager.ElementLocalDof[] GetDofOrder(Element targetElement)
        {
            return new FluentElementPermuteManager.ElementLocalDof[]
            {
                new FluentElementPermuteManager.ElementLocalDof(0, DoF.Dx),
                new FluentElementPermuteManager.ElementLocalDof(1, DoF.Dx),
            };
        }

        /// <inheritdoc/>
        public Matrix GetLocalInternalForceAt(Element targetElement, Displacement[] globalDisplacements, params double[] isoCoords)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public bool DoesOverrideKMatrixCalculation(Element targetElement, Matrix transformMatrix)
        {
            return false;
        }

        /// <inheritdoc/>
        public int[] GetNMaxOrder(Element targetElement)
        {
            return new int[] {1, 0, 0};
        }

        public int[] GetBMaxOrder(Element targetElement)
        {
            return new int[] {0, 0, 0};
        }

        public int[] GetDetJOrder(Element targetElement)
        {
            return new int[] { 0, 0, 0 };
        }

        public FlatShellStressTensor GetLoadInternalForceAt(Element targetElement, Load load, double[] isoLocation)
        {
            throw new NotImplementedException();
        }

        public FlatShellStressTensor GetLoadDisplacementAt(Element targetElement, Load load, double[] isoLocation)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Displacement GetLocalDisplacementAt(Element targetElement, Displacement[] localDisplacements, params double[] isoCoords)
        {
            throw new NotImplementedException();
        }

        public Force[] GetLocalEquivalentNodalLoads(Element targetElement, Load load)
        {
            var tr = targetElement.GetTransformationManager();

            #region uniform & trapezoid

            if (load is UniformLoad || load is TrapezoidalLoad)
            {

                Func<double, double> magnitude;
                Vector localDir;

                double xi0, xi1;
                int degree;//polynomial degree of magnitude function

                #region inits
                if (load is UniformLoad)
                {
                    var uld = (load as UniformLoad);

                    magnitude = (xi => uld.Magnitude);
                    localDir = uld.Direction;

                    if (uld.CoordinationSystem == CoordinationSystem.Global)
                        localDir = tr.TransformGlobalToLocal(localDir);

                    xi0 = -1;
                    xi1 = 1;
                    degree = 0;
                }
                else
                {
                    var tld = (load as TrapezoidalLoad);

                    magnitude = (xi => (load as TrapezoidalLoad).GetMagnitudesAt(xi, 0, 0)[0]);
                    localDir = tld.Direction;

                    if (tld.CoordinationSystem == CoordinationSystem.Global)
                        localDir = tr.TransformGlobalToLocal(localDir);

                    xi0 = tld.StarIsoLocations[0];
                    xi1 = tld.EndIsoLocations[0];
                    degree = 1;
                }

                localDir = localDir.GetUnit();
                #endregion

                {

                    var nOrd = GetNMaxOrder(targetElement).Max();

                    var gpt = (nOrd + degree) / 2 + 1;//gauss point count

                    var intg = GaussianIntegrator.CreateFor1DProblem(xi =>
                    {
                        var shp = GetNMatrixAt(targetElement, xi, 0, 0);
                        var q__ = magnitude(xi);
                        var j = GetJMatrixAt(targetElement, xi, 0, 0);
                        shp.MultiplyByConstant(j.Determinant());

                        var q_ = localDir * q__;

                        shp.MultiplyByConstant(q_.X);

                        return shp;
                    }, xi0, xi1, gpt);

                    var res = intg.Integrate();

                    var localForces = new Force[2];

                    var fx0 = res[0, 0];
                    var fx1 = res[0, 1];

                    localForces[0] = new Force(fx0, 0, 0, 0, 0, 0);
                    localForces[1] = new Force(fx1, 0, 0, 0, 0, 0);

                    return localForces;
                }
            }



            #endregion

            throw new NotImplementedException();

            
            #region uniform

            if (load is Loads.UniformLoad)
            {
                var ul = load as Loads.UniformLoad;

                var localDir = ul.Direction.GetUnit();

                if (ul.CoordinationSystem == CoordinationSystem.Global)
                {
                    localDir = tr.TransformGlobalToLocal(localDir);
                }

                var ux = localDir.X * ul.Magnitude;
                var uy = localDir.Y * ul.Magnitude;
                var uz = localDir.Z * ul.Magnitude;

                var intg = GaussianIntegrator.CreateFor1DProblem(xi =>
                {
                    var shp = GetNMatrixAt(targetElement, xi, 0, 0);
                    var j = GetJMatrixAt(targetElement, xi, 0, 0);
                    shp.MultiplyByConstant(j.Determinant());
                    shp.MultiplyByConstant(ux);

                    return shp;
                }, -1, 1, 2);

                var res = intg.Integrate();

                var localForces = new Force[2];

                {
                    var fx0 = res[0, 0];
                    var fx1 = res[0, 1];

                    localForces[0] = new Force(fx0, 0, 0, 0, 0, 0);
                    localForces[1] = new Force(fx1, 0, 0, 0, 0, 0);

                }

                var globalForces = localForces.Select(i => tr.TransformLocalToGlobal(i)).ToArray();
                return globalForces;

            }

            #endregion

            #region trapezoid

            if (load is TrapezoidalLoad)
            {
                var trLoad = load as TrapezoidalLoad;

                var localDir = trLoad.Direction;

                var startOffset = trLoad.StarIsoLocations[0];
                var endOffset = trLoad.EndIsoLocations[0];
                var startMag = trLoad.StartMagnitudes[0];
                var endMag = trLoad.EndMagnitudes[0];


                if (trLoad.CoordinationSystem == CoordinationSystem.Global)
                {
                    localDir = tr.TransformGlobalToLocal(localDir);
                }


                var xi0 = -1 + startOffset;
                var xi1 = 1 - endOffset;

                var intg = GaussianIntegrator.CreateFor1DProblem(xi =>
                {
                    var shp = GetNMatrixAt(targetElement, xi, 0, 0);
                    var q__ = trLoad.GetMagnitudesAt(xi)[0];
                    var j = GetJMatrixAt(targetElement, xi, 0, 0);
                    shp.MultiplyByConstant(j.Determinant());

                    var q_ = trLoad.Direction.GetUnit() * q__;

                    shp.MultiplyByConstant(q_.X);

                    return shp;
                }, xi0, xi1, 3);

                var res = intg.Integrate();

                var localForces = new Force[2];

                {
                    var fx0 = res[0, 0];
                    var fx1 = res[0, 1];

                    localForces[0] = new Force(fx0, 0, 0, 0, 0, 0);
                    localForces[1] = new Force(fx1, 0, 0, 0, 0, 0);
                }

                var globalForces = localForces.Select(i => tr.TransformLocalToGlobal(i)).ToArray();

                return globalForces;
            }

            #endregion
            
            
        }
    }
}