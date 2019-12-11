using Microsoft.VisualStudio.TestTools.UnitTesting;
using MazurPlanChecker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ESAPIX.Facade.API;
using ESAPIX.Constraints;

namespace MazurPlanChecker.Tests
{
    [TestClass()]
    public class CTDateConstraintTests
    {
        [TestMethod()]
        public void ConstrainTest()
        {

            //ARRANGE
            var im = new Image();
            im.CreationDateTime = DateTime.Now.AddDays(-59);

            //ACT
            var constraint = new CTDateConstraint();
            var actual = constraint.Constrain(im).ResultType;

            //ASSERT
            var expected = ResultType.PASSED;
            Assert.AreEqual(expected, actual);

        }
        [TestMethod()]
        public void ConstrainTestFails()
        {

            //ARRANGE
            var im = new Image();
            im.CreationDateTime = DateTime.Now.AddDays(-61);

            //ACT
            var constraint = new CTDateConstraint();
            var actual = constraint.Constrain(im).ResultType;

            //ASSERT
            var expected = ResultType.ACTION_LEVEL_3;
            Assert.AreEqual(expected, actual);

        }
    }
}