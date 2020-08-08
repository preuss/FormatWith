using System;
using System.Collections.Generic;
using System.Text;

namespace FormatWithTests.Values {
	public class ValueObject {
		public String Field = "Field";
		public String GetGetter() {
			return "Get Method";
		}
		public String AnotherMethod() {
			return "Method2";
		}
		public String Property => "Property Value";
	}

	public class PersonHolder {
		public Person Person => new Person("Kaj", 28);

		public PersonHolder() {
		}
	}
	public class Person {
		private string Name;
		public Person(string name, int age) {
			Name = name;
			Age = age;
		}
		public string getName() { return Name; }
		public int Age { get; set; }
	}
}
