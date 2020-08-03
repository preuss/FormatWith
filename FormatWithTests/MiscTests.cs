﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Xunit;
using FormatWith;
using static FormatWithTests.TestStrings;

namespace FormatWithTests {
	public class MiscTests {
		[Fact]
		public void TestMethodPassing() {
			// test to make sure testing framework is working (!)
			Assert.True(true);
		}

		[Fact]
		public void TestGetFormatParameters() {
			List<string> parameters = TestFormat4.GetFormatParameters().ToList();
			Assert.Equal(parameters.Count, 2);
			Assert.Equal(nameof(Replacement1), parameters[0]);
			Assert.Equal(nameof(Replacement2), parameters[1]);
		}
	}
}
