using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSCAD
{
	interface INSArc
	{
		UnitPoint	Center		{ get; }
		float		Radius		{ get; }
		float		StartAngle	{ get; }
		float		EndAngle	{ get; }
	}
}
