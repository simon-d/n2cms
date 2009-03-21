﻿using System;
using System.Diagnostics;

namespace N2.Web
{
	/// <summary>
	/// Used to bind a controller to a certain content type.
	/// </summary>
	[DebuggerDisplay("ControlsAttribute: {ItemType}->{ControllerType}")]
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
	public class ControlsAttribute : Attribute, IComparable<ControlsAttribute>, IAdapterDescriptor
	{
		private readonly Type itemType;
		private Type adapterType;

		public ControlsAttribute(Type itemType)
		{
			this.itemType = itemType;
		}

		public Type ItemType
		{
			get { return itemType; }
		}

		public Type AdapterType
		{
			get { return adapterType; }
			set { adapterType = value; }
		}

		public string ControllerName
		{
			get
			{
				string name = AdapterType.Name;
				int i = name.IndexOf("Controller");
				if (i > 0)
				{
					return name.Substring(0, i);
				}
				return name;
			}
		}

		public bool IsAdapterFor(PathData path, Type requiredType)
		{
			if (path.IsEmpty())
				return false;

			return ItemType.IsAssignableFrom(path.CurrentItem.GetType()) && requiredType.IsAssignableFrom(requiredType);
		}

		#region IComparable<IControllerReference> Members

		public int CompareTo(IAdapterDescriptor other)
		{
			if (other.ItemType.IsSubclassOf(ItemType))
				return DistanceBetween(ItemType, other.ItemType);
			if (ItemType.IsSubclassOf(other.ItemType))
				return -1 * DistanceBetween(other.ItemType, ItemType);

			return 0;
		}

		int DistanceBetween(Type super, Type sub)
		{
			int distance = 1;
			for (Type t = sub; t != super; t = t.BaseType)
			{
				++distance;
			}
			return distance;
		}

		#endregion

		#region IComparable<ControlsAttribute> Members

		int IComparable<ControlsAttribute>.CompareTo(ControlsAttribute other)
		{
			return CompareTo(other);
		}

		#endregion
	}
}
