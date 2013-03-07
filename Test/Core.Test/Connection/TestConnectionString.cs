//===========================================================================
// MODULE:  TestConnectionString.cs
// PURPOSE: connection string unit test
// 
// Copyright © 2013
// Brent M. Spell. All rights reserved.
//
// This library is free software; you can redistribute it and/or modify it 
// under the terms of the GNU Lesser General Public License as published 
// by the Free Software Foundation; either version 3 of the License, or 
// (at your option) any later version. This library is distributed in the 
// hope that it will be useful, but WITHOUT ANY WARRANTY; without even the 
// implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
// See the GNU Lesser General Public License for more details. You should 
// have received a copy of the GNU Lesser General Public License along with 
// this library; if not, write to 
//    Free Software Foundation, Inc. 
//    51 Franklin Street, Fifth Floor 
//    Boston, MA 02110-1301 USA
//===========================================================================
// System References
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
// Project References

namespace SkyFloe.Core.Test.Connection
{
   [TestClass]
   public class TestConnectionString
   {
      [TestMethod]
      public void TestConstruction ()
      {
         var cstr = (ConnectionString)null;
         // invalid construction
         AssertException(() => new ConnectionString(null, null));
         AssertException(() => new ConnectionString("test", null));
         AssertException(
            () => new ConnectionString(
               null, 
               new KeyValuePair<String, String>[0]
            )
         );
         AssertException(
            () => new ConnectionString(
               "test",
               new[] { new KeyValuePair<String, String>(null, "value") }
            )
         );
         AssertException(
            () => new ConnectionString(
               "test",
               new[] { new KeyValuePair<String, String>("", "value") }
            )
         );
         AssertException(
            () => new ConnectionString(
               "test",
               new[] { new KeyValuePair<String, String>(" ", "value") }
            )
         );
         // valid construction
         cstr = new ConnectionString(
            "test",
            new KeyValuePair<String, String>[0]
         );
         Assert.AreEqual(cstr.Store, "test");
         Assert.IsFalse(cstr.Properties.Any());
         cstr = new ConnectionString(
            "test",
            new []
            {
               new KeyValuePair<String, String>("test1", null),
               new KeyValuePair<String, String>("test2", "value2")
            }
         );
         Assert.AreEqual(cstr.Properties.Count(), 2);
         Assert.IsNull(cstr.Properties.ToDictionary()["test1"]);
         Assert.AreEqual(cstr.Properties.ToDictionary()["test2"], "value2");
         cstr = new ConnectionString(
            "test",
            new[]
            {
               new KeyValuePair<String, String>("test1", "value1"),
               new KeyValuePair<String, String>("test1", "value2")
            }
         );
         Assert.AreEqual(cstr.Properties.Count(), 1);
         Assert.AreEqual(cstr.Properties.ToDictionary()["test1"], "value2");
      }

      [TestMethod]
      public void TestParse ()
      {
         var cstr = (ConnectionString)null;
         // invalid connection strings
         AssertException(() => ConnectionString.Parse(null));
         AssertException(() => ConnectionString.Parse(""));
         AssertException(() => ConnectionString.Parse(" "));
         AssertException(() => ConnectionString.Parse("Property=value"));
         // minimal connection string
         cstr = ConnectionString.Parse("Store=Test");
         Assert.AreEqual(cstr.Store, "Test");
         cstr = ConnectionString.Parse(" Store = Test ");
         Assert.AreEqual(cstr.Store, "Test");
         cstr = ConnectionString.Parse(" Store = Test; ");
         Assert.AreEqual(cstr.Store, "Test");
         cstr = ConnectionString.Parse(" Store = Test ;");
         Assert.AreEqual(cstr.Store, "Test");
         // invalid properties
         cstr = ConnectionString.Parse("Store=Test;Prop");
         Assert.AreEqual(cstr.Store, "Test");
         Assert.IsFalse(cstr.Properties.Any());
         cstr = ConnectionString.Parse("Store=Test;Prop;");
         Assert.AreEqual(cstr.Store, "Test");
         Assert.IsFalse(cstr.Properties.Any());
         cstr = ConnectionString.Parse("Store=Test;Pro#=value");
         Assert.AreEqual(cstr.Store, "Test");
         Assert.IsFalse(cstr.Properties.Any());
         cstr = ConnectionString.Parse("Store=Test;Prop=\"value");
         Assert.AreEqual(cstr.Store, "Test");
         Assert.IsFalse(cstr.Properties.Any());
         cstr = ConnectionString.Parse("Store=Test;Prop1=value1;Prop2");
         Assert.AreEqual(cstr.Store, "Test");
         Assert.AreEqual(cstr.Properties.Count(), 1);
         cstr = ConnectionString.Parse("Store=Test;Prop1=value1;Prop2;");
         Assert.AreEqual(cstr.Store, "Test");
         Assert.AreEqual(cstr.Properties.Count(), 1);
         // unquoted properties
         cstr = ConnectionString.Parse("Store=Test;Prop=");
         Assert.AreEqual(cstr.Properties.Count(), 1);
         Assert.AreEqual(cstr.Properties.ToDictionary()["Prop"], "");
         cstr = ConnectionString.Parse("Store=Test;Prop=;");
         Assert.AreEqual(cstr.Properties.Count(), 1);
         Assert.AreEqual(cstr.Properties.ToDictionary()["Prop"], "");
         cstr = ConnectionString.Parse("Prop=;Store=Test;");
         Assert.AreEqual(cstr.Store, "Test");
         Assert.AreEqual(cstr.Properties.Count(), 1);
         Assert.AreEqual(cstr.Properties.ToDictionary()["Prop"], "");
         cstr = ConnectionString.Parse("Store=Test;Prop=;;");
         Assert.AreEqual(cstr.Properties.Count(), 1);
         Assert.AreEqual(cstr.Properties.ToDictionary()["Prop"], "");
         cstr = ConnectionString.Parse("Store=Test;Prop=      ;");
         Assert.AreEqual(cstr.Properties.Count(), 1);
         Assert.AreEqual(cstr.Properties.ToDictionary()["Prop"], "");
         cstr = ConnectionString.Parse("Store=Test;Prop1=;Prop2=");
         Assert.AreEqual(cstr.Properties.Count(), 2);
         Assert.AreEqual(cstr.Properties.ToDictionary()["Prop1"], "");
         Assert.AreEqual(cstr.Properties.ToDictionary()["Prop2"], "");
         cstr = ConnectionString.Parse("Store=Test;Prop=value");
         Assert.AreEqual(cstr.Properties.Count(), 1);
         Assert.AreEqual(cstr.Properties.ToDictionary()["Prop"], "value");
         cstr = ConnectionString.Parse("Store=Test;Prop=value1;Prop=value2");
         Assert.AreEqual(cstr.Properties.Count(), 1);
         Assert.AreEqual(cstr.Properties.ToDictionary()["Prop"], "value2");
         cstr = ConnectionString.Parse("Store=Test;Prop = value ");
         Assert.AreEqual(cstr.Properties.Count(), 1);
         Assert.AreEqual(cstr.Properties.ToDictionary()["Prop"], "value");
         cstr = ConnectionString.Parse("Store=Test;Prop = value ;");
         Assert.AreEqual(cstr.Properties.Count(), 1);
         Assert.AreEqual(cstr.Properties.ToDictionary()["Prop"], "value");
         cstr = ConnectionString.Parse("Store=Test;Prop1 = ; Prop2 = value2");
         Assert.AreEqual(cstr.Properties.Count(), 2);
         Assert.AreEqual(cstr.Properties.ToDictionary()["Prop1"], "");
         Assert.AreEqual(cstr.Properties.ToDictionary()["Prop2"], "value2");
         cstr = ConnectionString.Parse("Store=Test;Prop1 = value1 ; Prop2 =  ");
         Assert.AreEqual(cstr.Properties.Count(), 2);
         Assert.AreEqual(cstr.Properties.ToDictionary()["Prop1"], "value1");
         Assert.AreEqual(cstr.Properties.ToDictionary()["Prop2"], "");
         cstr = ConnectionString.Parse("Store=Test;Prop1 = value1 ; Prop2 =  ;");
         Assert.AreEqual(cstr.Properties.Count(), 2);
         Assert.AreEqual(cstr.Properties.ToDictionary()["Prop1"], "value1");
         Assert.AreEqual(cstr.Properties.ToDictionary()["Prop2"], "");
         cstr = ConnectionString.Parse("Store=Test;Prop1 = value1 ; Prop2 = value2");
         Assert.AreEqual(cstr.Properties.Count(), 2);
         Assert.AreEqual(cstr.Properties.ToDictionary()["Prop1"], "value1");
         Assert.AreEqual(cstr.Properties.ToDictionary()["Prop2"], "value2");
         // quoted properties
         cstr = ConnectionString.Parse("Store=Test;Prop=\"\";");
         Assert.AreEqual(cstr.Properties.Count(), 1);
         Assert.AreEqual(cstr.Properties.ToDictionary()["Prop"], "");
         cstr = ConnectionString.Parse("Store=Test;Prop=\" \";");
         Assert.AreEqual(cstr.Properties.Count(), 1);
         Assert.AreEqual(cstr.Properties.ToDictionary()["Prop"], " ");
         cstr = ConnectionString.Parse("Store=Test;Prop=\";\"");
         Assert.AreEqual(cstr.Properties.Count(), 1);
         Assert.AreEqual(cstr.Properties.ToDictionary()["Prop"], ";");
         cstr = ConnectionString.Parse("Store=Test;Prop=\" ; \";");
         Assert.AreEqual(cstr.Properties.Count(), 1);
         Assert.AreEqual(cstr.Properties.ToDictionary()["Prop"], " ; ");
         cstr = ConnectionString.Parse("Store=Test;Prop1=\"\"\"\";Prop2 = \"\"\"\"\"\"");
         Assert.AreEqual(cstr.Properties.Count(), 2);
         Assert.AreEqual(cstr.Properties.ToDictionary()["Prop1"], "\"");
         Assert.AreEqual(cstr.Properties.ToDictionary()["Prop2"], "\"\"");
         cstr = ConnectionString.Parse("Store=Test;Prop1=\"\"\";\";Prop2 = \"\"\" ; \"\"\"");
         Assert.AreEqual(cstr.Properties.Count(), 2);
         Assert.AreEqual(cstr.Properties.ToDictionary()["Prop1"], "\";");
         Assert.AreEqual(cstr.Properties.ToDictionary()["Prop2"], "\" ; \"");
      }

      [TestMethod]
      public void TestFormat ()
      {
         Assert.AreEqual(
            ConnectionString.Parse("Store=Test;Prop=value;").ToString(),
            "Store=Test;Prop=value;"
         );
         Assert.AreEqual(
            ConnectionString.Parse("Prop=value;Store=Test;").ToString(),
            "Store=Test;Prop=value;"
         );
         Assert.AreEqual(
            ConnectionString.Parse("Store=Test;Prop=value").ToString(),
            "Store=Test;Prop=value;"
         );
         Assert.AreEqual(
            ConnectionString.Parse("Store = Test ; Prop = value ;").ToString(),
            "Store=Test;Prop=value;"
         );
         Assert.AreEqual(
            ConnectionString.Parse("Store=Test; Prop1 = value1 ; Prop2=value2").ToString(),
            "Store=Test;Prop1=value1;Prop2=value2;"
         );
         Assert.AreEqual(
            ConnectionString.Parse("Store=Test;Prop=\"\"").ToString(),
            "Store=Test;Prop=;"
         );
         Assert.AreEqual(
            ConnectionString.Parse("Store=Test;Prop=\"value\"").ToString(),
            "Store=Test;Prop=value;"
         );
         Assert.AreEqual(
            ConnectionString.Parse("Store=Test;Prop=\" \"").ToString(),
            "Store=Test;Prop=\" \";"
         );
         Assert.AreEqual(
            ConnectionString.Parse("Store=Test;Prop=\" value\"").ToString(),
            "Store=Test;Prop=\" value\";"
         );
         Assert.AreEqual(
            ConnectionString.Parse("Store=Test;Prop=\"value \"").ToString(),
            "Store=Test;Prop=\"value \";"
         );
         Assert.AreEqual(
            ConnectionString.Parse("Store=Test;Prop=\"\"\"\"").ToString(),
            "Store=Test;Prop=\"\"\"\";"
         );
         Assert.AreEqual(
            ConnectionString.Parse("Store=Test;Prop=\"\"\" \"\"\"").ToString(),
            "Store=Test;Prop=\"\"\" \"\"\";"
         );
         Assert.AreEqual(
            ConnectionString.Parse("Store=Test;Prop=\";\"").ToString(),
            "Store=Test;Prop=\";\";"
         );
         Assert.AreEqual(
            ConnectionString.Parse("Store=Test;Prop=\";;\"").ToString(),
            "Store=Test;Prop=\";;\";"
         );
         Assert.AreEqual(
            ConnectionString.Parse("Store=Test;Prop=\"\"\";\"\"\"").ToString(),
            "Store=Test;Prop=\"\"\";\"\"\";"
         );
      }

      [TestMethod]
      public void TestExtract ()
      {
         var connect = (ConnectionString)null;
         // invalid extraction
         AssertException(() => ConnectionString.Extract(null));
         // empty extraction
         connect = ConnectionString.Extract(new EmptyStore());
         Assert.AreEqual(connect.Store, typeof(EmptyStore).AssemblyQualifiedName);
         Assert.IsFalse(connect.Properties.Any());
         // valid extraction
         connect = ConnectionString.Extract(
            new TestStore()
            {
               Property1 = 42
            }
         );
         Assert.AreEqual(connect.Store, typeof(TestStore).AssemblyQualifiedName);
         Assert.AreEqual(connect.Properties.Count(), 1);
         Assert.AreEqual(connect.Properties.ToDictionary()["Property1"], "42");
         connect = ConnectionString.Extract(
            new TestStore()
            {
               Property1 = 42,
               Property2 = ""
            }
         );
         Assert.AreEqual(connect.Store, typeof(TestStore).AssemblyQualifiedName);
         Assert.AreEqual(connect.Properties.Count(), 2);
         Assert.AreEqual(connect.Properties.ToDictionary()["Property1"], "42");
         Assert.AreEqual(connect.Properties.ToDictionary()["Property2"], "");
         connect = ConnectionString.Extract(
            new TestStore()
            {
               Property1 = 42,
               Property2 = "value2"
            }
         );
         Assert.AreEqual(connect.Store, typeof(TestStore).AssemblyQualifiedName);
         Assert.AreEqual(connect.Properties.Count(), 2);
         Assert.AreEqual(connect.Properties.ToDictionary()["Property1"], "42");
         Assert.AreEqual(connect.Properties.ToDictionary()["Property2"], "value2");
      }

      [TestMethod]
      public void TestBind ()
      {
         var store = (TestStore)null;
         // invalid bind
         AssertException(
            () => ConnectionString.Parse("Store=Test;Prop=value;").Bind(null)
         );
         AssertException(
            () => ConnectionString.Parse("Store=Test;Prop=value;").Bind(new EmptyStore())
         );
         AssertException(
            () => ConnectionString.Parse("Store=Test;Property3=value;").Bind(new TestStore())
         );
         AssertException(
            () => ConnectionString.Parse("Store=Test;Property1=invalid;").Bind(new TestStore())
         );
         // valid bind
         store = new TestStore() { Property1 = 42, Property2 = "value2" };
         ConnectionString.Parse("Store=Test;").Bind(store);
         Assert.AreEqual(store.Property1, 42);
         Assert.AreEqual(store.Property2, "value2");
         store = new TestStore() { Property1 = 42, Property2 = "value2" };
         ConnectionString.Parse("Store=Test;Property1=43;").Bind(store);
         Assert.AreEqual(store.Property1, 43);
         Assert.AreEqual(store.Property2, "value2");
         store = new TestStore() { Property1 = 42, Property2 = "value2" };
         ConnectionString.Parse("Store=Test;pRoPeRtY1=43;PrOpErTy2=value3;").Bind(store);
         Assert.AreEqual(store.Property1, 43);
         Assert.AreEqual(store.Property2, "value3");
      }

      [TestMethod]
      public void TestComparison ()
      {
         // invalid comparison
         Assert.IsFalse(ConnectionString.Parse("Store=test;").Equals(42));
         // cstr-cstr comparison (null-valid)
         {
            var cstr1 = (ConnectionString)null;
            var cstr2 = (ConnectionString)null;
            var cstr3 = ConnectionString.Parse("Store=Test;Prop=value;");
            Assert.IsTrue(cstr1 == cstr2);
            Assert.IsFalse(cstr1 != cstr2);
            Assert.IsFalse(cstr1 == cstr3);
            Assert.IsTrue(cstr1 != cstr3);
         }
         // cstr-cstr comparison (valid-valid, store name)
         {
            var cstr1 = ConnectionString.Parse("Store=Test1;Property1=value1;Property2=value2;");
            var cstr2 = ConnectionString.Parse("Store=Test1;Property1=value1;Property2=value2;");
            var cstr3 = ConnectionString.Parse("Store=test1;Property1=value1;Property2=value2;");
            Assert.IsTrue(cstr1.Equals((Object)cstr2));
            Assert.IsTrue(cstr1.Equals(cstr2));
            Assert.IsTrue(cstr1 == cstr2);
            Assert.IsFalse(cstr1 != cstr2);
            Assert.IsFalse(cstr1.Equals((Object)cstr3));
            Assert.IsFalse(cstr1.Equals(cstr3));
            Assert.IsFalse(cstr1 == cstr3);
            Assert.IsTrue(cstr1 != cstr3);
         }
         // cstr-cstr comparison (valid-valid, property count)
         {
            var cstr1 = ConnectionString.Parse("Store=Test;Property1=value1;");
            var cstr2 = ConnectionString.Parse("StOrE=Test;PrOpErTy1=value1;");
            var cstr3 = ConnectionString.Parse("Store=Test;Property1=value1;Property2=value2;");
            Assert.IsTrue(cstr1.Equals((Object)cstr2));
            Assert.IsTrue(cstr1.Equals(cstr2));
            Assert.IsTrue(cstr1 == cstr2);
            Assert.IsFalse(cstr1 != cstr2);
            Assert.IsFalse(cstr1.Equals((Object)cstr3));
            Assert.IsFalse(cstr1.Equals(cstr3));
            Assert.IsFalse(cstr1 == cstr3);
            Assert.IsTrue(cstr1 != cstr3);
         }
         // cstr-cstr comparison (valid-valid, property value)
         {
            var cstr1 = ConnectionString.Parse("Store=Test;Property1=value1;Property2=value2;");
            var cstr2 = ConnectionString.Parse("StOrE=Test;PrOpErTy1=value1;PrOpErTy2=value2;");
            var cstr3 = ConnectionString.Parse("Store=Test;Property1=value1;Property2=value3;");
            var cstr4 = ConnectionString.Parse("Store=Test;Property3=value5;Property4=value6;");
            Assert.IsTrue(cstr1.Equals((Object)cstr2));
            Assert.IsTrue(cstr1.Equals(cstr2));
            Assert.IsTrue(cstr1 == cstr2);
            Assert.IsFalse(cstr1 != cstr2);
            Assert.IsFalse(cstr1.Equals((Object)cstr3));
            Assert.IsFalse(cstr1.Equals(cstr3));
            Assert.IsFalse(cstr1 == cstr3);
            Assert.IsTrue(cstr1 != cstr3);
            Assert.IsFalse(cstr1.Equals((Object)cstr4));
            Assert.IsFalse(cstr1.Equals(cstr4));
            Assert.IsFalse(cstr1 == cstr4);
            Assert.IsTrue(cstr1 != cstr4);
         }
         // cstr-cstr comparison (valid-null)
         {
            var cstr1 = ConnectionString.Parse("Store=Test;Prop=value;");
            var cstr2 = (ConnectionString)null;
            Assert.IsFalse(cstr1.Equals((Object)cstr2));
            Assert.IsFalse(cstr1.Equals(cstr2));
            Assert.IsFalse(cstr1 == cstr2);
            Assert.IsTrue(cstr1 != cstr2);
         }
         // cstr-str comparison (null-valid)
         {
            var cstr = (ConnectionString)null;
            var str1 = (String)null;
            var str2 = String.Empty;
            var str3 = " ";
            var str4 = "Store=Test;Prop=value;";
            Assert.IsTrue(cstr == str1);
            Assert.IsFalse(cstr != str1);
            Assert.IsTrue(str1 == cstr);
            Assert.IsFalse(str1 != cstr);
            Assert.IsTrue(cstr == str2);
            Assert.IsFalse(cstr != str2);
            Assert.IsTrue(str2 == cstr);
            Assert.IsFalse(str2 != cstr);
            Assert.IsTrue(cstr == str3);
            Assert.IsFalse(cstr != str3);
            Assert.IsTrue(str3 == cstr);
            Assert.IsFalse(str3 != cstr);
            Assert.IsFalse(cstr == str4);
            Assert.IsTrue(cstr != str4);
            Assert.IsFalse(str4 == cstr);
            Assert.IsTrue(str4 != cstr);
         }
         // cstr-str comparison (valid-valid)
         {
            var cstr = ConnectionString.Parse("Store=Test;Property1=value1;Property2=value2;");
            var str1 = "StOrE=Test;PrOpErTy1=value1;PrOpErTy2 = value2 ";
            var str2 = "Store=Test;Property1=value1;Property2=value3;";
            Assert.IsTrue(cstr.Equals((Object)str1));
            Assert.IsTrue(cstr.Equals(str1));
            Assert.IsTrue(cstr == str1);
            Assert.IsFalse(cstr != str1);
            Assert.IsTrue(str1 == cstr);
            Assert.IsFalse(str1 != cstr);
            Assert.IsFalse(cstr.Equals((Object)str2));
            Assert.IsFalse(cstr.Equals(str2));
            Assert.IsFalse(cstr == str2);
            Assert.IsTrue(cstr != str2);
            Assert.IsFalse(str2 == cstr);
            Assert.IsTrue(str2 != cstr);
         }
         // cstr-str comparison (valid-null)
         {
            var cstr = ConnectionString.Parse("Store=Test;Prop=value;");
            var str1 = (String)null;
            var str2 = String.Empty;
            var str3 = " ";
            Assert.IsFalse(cstr.Equals(str1));
            Assert.IsFalse(cstr.Equals((Object)str1));
            Assert.IsFalse(cstr == str1);
            Assert.IsTrue(cstr != str1);
            Assert.IsFalse(str1 == cstr);
            Assert.IsTrue(str1 != cstr);
            Assert.IsFalse(cstr.Equals((Object)str2));
            Assert.IsFalse(cstr.Equals(str2));
            Assert.IsFalse(cstr == str2);
            Assert.IsTrue(cstr != str2);
            Assert.IsFalse(str2 == cstr);
            Assert.IsTrue(str2 != cstr);
            Assert.IsFalse(cstr.Equals((Object)str3));
            Assert.IsFalse(cstr.Equals(str3));
            Assert.IsFalse(cstr == str3);
            Assert.IsTrue(cstr != str3);
            Assert.IsFalse(str3 == cstr);
            Assert.IsTrue(str3 != cstr);
         }
         // hash code calculation
         {
            Assert.AreEqual(
               ConnectionString.Parse("Store=Test;Prop=value;").GetHashCode(),
               ConnectionString.Parse("Store=Test;Prop=value;").GetHashCode()
            );
            Assert.AreEqual(
               ConnectionString.Parse("Store=Test;Prop=value;").GetHashCode(),
               ConnectionString.Parse("StOrE = Test ; PrOp = value  ;").GetHashCode()
            );
            Assert.AreNotEqual(
               ConnectionString.Parse("Store=Test1;Prop=value;").GetHashCode(),
               ConnectionString.Parse("Store=Test2;Prop=value;").GetHashCode()
            );
            Assert.AreNotEqual(
               ConnectionString.Parse("Store=Test;Prop=value1;").GetHashCode(),
               ConnectionString.Parse("Store=Test;Prop=value2;").GetHashCode()
            );
            Assert.AreNotEqual(
               ConnectionString.Parse("Store=Test1;Prop=\" \";").GetHashCode(),
               ConnectionString.Parse("Store=Test1;Prop=\"\";").GetHashCode()
            );
            Assert.AreNotEqual(
               ConnectionString.Parse("Store=Test;Property1=value1").GetHashCode(),
               ConnectionString.Parse("Store=Test;Property1=value1;Property2=value2;").GetHashCode()
            );
            Assert.AreNotEqual(
               ConnectionString.Parse("Store=Test;Property1=value1;Property2=value2;").GetHashCode(),
               ConnectionString.Parse("Store=Test;Property1=value1;Property2=value3;").GetHashCode()
            );
         }
      }

      [TestMethod]
      public void TestConversion ()
      {
         // cstr-str conversion
         {
            var cstr = (ConnectionString)null;
            String str = cstr;
            Assert.IsNull(str);
         }
         {
            var cstr = ConnectionString.Parse("Store=Test;Prop=value;");
            String str = cstr;
            Assert.AreEqual(str, "Store=Test;Prop=value;");
         }
         // str-cstr conversion
         {
            var str = (String)null;
            var cstr = (ConnectionString)str;
            Assert.IsNull(cstr);
         }
         {
            var str = String.Empty;
            var cstr = (ConnectionString)str;
            Assert.IsNull(cstr);
         }
         {
            var str = " ";
            var cstr = (ConnectionString)str;
            Assert.IsNull(cstr);
         }
         {
            var str = "Store=Test;Prop=value;";
            var cstr = (ConnectionString)str;
            Assert.AreEqual(cstr, ConnectionString.Parse(str));
         }
      }

      private class EmptyStore
      {
      }

      private class TestStore
      {
         public Int32 Property1 { get; set; }
         public String Property2 { get; set; }
      }

      private void AssertException (Action a)
      {
         try { a(); }
         catch { return; }
         Assert.Fail("Expected: exception");
      }
   }
}
