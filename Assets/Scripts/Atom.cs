
using System;
using UnityEngine;
namespace AssemblyCSharp
{
	public struct Atom
	{
		public Vector3 position;
		public string name;


		public Atom(Vector3 position, string name){
			this.position = position;
			this.name = name;
		}

		private float distance(Vector3 position){
			return Vector3.Distance(this.position,position);
		}
	}
}

