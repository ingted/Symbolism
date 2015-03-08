﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Symbolism.CoefficientGpe;
using Symbolism.AlgebraicExpand;

namespace Symbolism.IsolateVariable
{
    public static class Extensions
    {
        public static MathObject IsolateVariableEq(this Equation eq, Symbol sym)
        {
            if (eq.FreeOf(sym)) return eq;

            if (eq.b.Has(sym)) return IsolateVariableEq(new Equation(eq.a - eq.b, 0), sym);

            if (eq.a == sym) return eq;

            // (a x^2 + c) / x == - b

            if (eq.a is Product &&
                (eq.a as Product).elts.Any(
                    elt =>
                        elt is Power &&
                        (elt as Power).bas == sym &&
                        (elt as Power).exp == -1))
                return IsolateVariableEq(eq.a * sym == eq.b * sym, sym);

            //if (eq.a is Product &&
            //    (eq.a as Product).elts.Any(
            //        elt =>
            //            elt is Power &&
            //            (elt as Power).bas == sym &&
            //            (elt as Power).exp is Integer &&
            //            ((elt as Power).exp as Integer).val < 0))
            //    return IsolateVariableEq(eq.a * sym == eq.b * sym, sym);


            // if (eq.a.Denominator() is Product &&
            //     (eq.a.Denominator() as Product).Any(elt => elt.Base() == sym)
            // 
            // 


            // (x + y)^(1/2) == z
            //
            // x == -y + z^2   &&   z >= 0

            if (eq.a is Power && (eq.a as Power).exp == new Integer(1) / 2)
                return IsolateVariableEq((eq.a ^ 2) == (eq.b ^ 2), sym);

            if (eq.a.AlgebraicExpand().DegreeGpe(new List<MathObject>() { sym }) == 2)
            {
                var a = eq.a.AlgebraicExpand().CoefficientGpe(sym, 2);
                var b = eq.a.AlgebraicExpand().CoefficientGpe(sym, 1);
                var c = eq.a.AlgebraicExpand().CoefficientGpe(sym, 0);

                return new Or(

                    new And(
                        sym == (-b + (((b ^ 2) - 4 * a * c) ^ (new Integer(1) / 2))) / (2 * a),
                        (a != 0).Simplify()
                        ).Simplify(),

                    new And(
                        sym == (-b - (((b ^ 2) - 4 * a * c) ^ (new Integer(1) / 2))) / (2 * a),
                        (a != 0).Simplify()
                        ).Simplify(),

                    new And(sym == -c / b, a == 0, (b != 0).Simplify()).Simplify(),

                    new And(
                        (a == 0).Simplify(),
                        (b == 0).Simplify(),
                        (c == 0).Simplify()
                        ).Simplify()

                ).Simplify();
            }


            // (x + y == z).IsolateVariable(x)

            if (eq.a is Sum && (eq.a as Sum).elts.Any(elt => elt.FreeOf(sym)))
            {
                var items = ((Sum)eq.a).elts.FindAll(elt => elt.FreeOf(sym));

                //return IsolateVariable(
                //    new Equation(
                //        eq.a - new Sum() { elts = items }.Simplify(),
                //        eq.b - new Sum() { elts = items }.Simplify()),
                //    sym);


                //var new_a = eq.a; items.ForEach(elt => new_a = new_a - elt);
                //var new_b = eq.b; items.ForEach(elt => new_b = new_b - elt);

                var new_a = new Sum() { elts = (eq.a as Sum).elts.Where(elt => items.Contains(elt) == false).ToList() }.Simplify();
                var new_b = eq.b; items.ForEach(elt => new_b = new_b - elt);

                // (new_a as Sum).Where(elt => items.Contains(elt) == false)

                return IsolateVariableEq(new Equation(new_a, new_b), sym);

                //return IsolateVariable(
                //    new Equation(
                //        eq.a + new Sum() { elts = items.ConvertAll(elt => elt * -1) }.Simplify(),
                //        eq.b - new Sum() { elts = items }.Simplify()),
                //    sym);
            }

            // a b + a c == d

            // a + a c == d

            if (eq.a is Sum && (eq.a as Sum).elts.All(elt => elt.DegreeGpe(new List<MathObject>() { sym }) == 1))
            {
                //return 
                //    (new Sum() { elts = (eq.a as Sum).elts.Select(elt => elt / sym).ToList() }.Simplify() == eq.b / sym)
                //    .IsolateVariable(sym);

                return
                    (sym * new Sum() { elts = (eq.a as Sum).elts.Select(elt => elt / sym).ToList() }.Simplify() == eq.b)
                    .IsolateVariable(sym);
            }

            if (eq.a is Sum) return eq.AlgebraicExpand().IsolateVariable(sym);

            if (eq.a is Product)
            {
                var items = ((Product)eq.a).elts.FindAll(elt => elt.FreeOf(sym));

                return IsolateVariableEq(
                    new Equation(
                        eq.a / new Product() { elts = items }.Simplify(),
                        eq.b / new Product() { elts = items }.Simplify()),
                    sym);
            }

            throw new Exception();
        }

        public static MathObject IsolateVariable(this MathObject obj, Symbol sym)
        {
            if (obj is Or) return new Or() { args = (obj as Or).args.Select(elt => elt.IsolateVariable(sym)).ToList() }.Simplify();

            if (obj is And) return new And() { args = (obj as And).args.Select(elt => elt.IsolateVariable(sym)).ToList() }.Simplify();

            if (obj is Equation) return (obj as Equation).IsolateVariableEq(sym);

            throw new Exception();
        }
    }
}
