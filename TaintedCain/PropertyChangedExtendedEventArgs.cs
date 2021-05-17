using System.ComponentModel;

namespace TaintedCain
{
	public class PropertyChangedExtendedEventArgs<T> : PropertyChangedEventArgs
	{
		public virtual T OldValue { get; private set; }
		public virtual T NewValue { get; private set; }

		public PropertyChangedExtendedEventArgs(string property_name, T old_value, T new_value)
			: base(property_name)
		{
			OldValue = old_value;
			NewValue = new_value;
		}
	}
}