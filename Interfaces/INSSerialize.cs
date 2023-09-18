using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace NSCAD
{
	interface INSSerialize
	{
		void GetObjectData(XmlWriter wr);
		void AfterSerializedIn();
	}
}
