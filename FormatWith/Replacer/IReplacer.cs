﻿using System;
using System.Collections.Generic;
using System.Text;

namespace FormatWith.Replacer {
	interface IReplacer {
		string Format(string parameter, ReplacementArgument argument);
	}
}
