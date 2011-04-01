using System;
using System.Linq;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using System.ComponentModel.Composition;

namespace OpenTK.Structure
{
	public class QuadTree<T>
	{
		public Vector2 Min;
		public Vector2 Max;
		public int Depth;

		public List<QuadTree<T>> Children
		{
			get;
			private set;
		}
		public List<T> Payload
		{
			get;
			private set;
		}

		public QuadTree ()
		{
			Children = new List<QuadTree<T>> ();
			Payload = new List<T> ();
		}

		public void Split (Func<QuadTree<T>, Vector2> centerSelector, Action<QuadTree<T>> payloadSplitter, Func<QuadTree<T>, bool> terminationCondition)
		{
			if (terminationCondition (this))
				return;
			
			var center = centerSelector (this);
			
			var q11 = new QuadTree<T> { Min = Min, Max = center, Depth = Depth + 1 };
			var q12 = new QuadTree<T> { Min = new Vector2 (center.X, Min.Y), Max = new Vector2 (Max.X, center.Y), Depth = Depth + 1 };
			var q21 = new QuadTree<T> { Min = new Vector2 (Min.X, center.Y), Max = new Vector2 (center.X, Max.Y), Depth = Depth + 1 };
			var q22 = new QuadTree<T> { Min = center, Max = Max, Depth = Depth + 1 };
			
			Children.Add (q11);
			Children.Add (q12);
			Children.Add (q21);
			Children.Add (q22);
			
			payloadSplitter (this);
			
			foreach (var item in Children)
			{
				item.Split (centerSelector, payloadSplitter, terminationCondition);
			}
		}

		public void Traverse (Action<QuadTree<T>> visitor, Func<QuadTree<T>, bool> navigator)
		{
			visitor (this);
			
			foreach (var item in Children)
			{
				if (navigator (item))
					item.Traverse (visitor, navigator);
			}
		}
	}
}

