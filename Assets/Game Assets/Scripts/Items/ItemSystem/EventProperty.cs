using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Rair.Events
{
	public enum MergeType { None, Sum, Average, Max, Min, Mode, Median }
	public abstract class EventProperty<T> where T : struct
	{
		protected T value;
		protected MergeType Option { get; set; }
		protected bool AllowDefault { get; set; }

		#region events
		public delegate void ValueAction(T oldValue, T newValue);
		/// <summary>이전 값과 신규 값을 매개로 실행되는 이벤트 함수</summary>
		public event ValueAction OnValueChanged;
		public delegate void RefFunc(ref T value);
		protected event RefFunc ModifierDelegates;
		public event RefFunc Modifier
		{
			add
			{
				T old = Value;
				ModifierDelegates += value;
				if (old.Equals(Value) is false) OnValueChanged?.Invoke(old, Value);
			}
			remove
			{
				T old = Value;
				ModifierDelegates -= value;
				if (old.Equals(Value) is false) OnValueChanged?.Invoke(old, Value);
			}
		}
		#endregion

		public T Value
		{
			set
			{
				T old = Value;
				this.value = value;
				if (old.Equals(Value) is false) OnValueChanged?.Invoke(this.value, value);
			}
			get
			{
				T value = this.value;
				ModifierDelegates?.Invoke(ref value);
				return value;
			}
		}
		public EventProperty(T value, MergeType option, bool allowDefault)
		{
			this.value = value;
			Option = option;
			AllowDefault = allowDefault;
		}
		public abstract void Merge(IEnumerable<EventProperty<T>> values);

		public override string ToString() => value.ToString();
		public static implicit operator T(EventProperty<T> eValue) => eValue.Value;
	}

  #region Event Property Derived Classes
	[Serializable]
	public class EBool : EventProperty<bool>
	{
		public EBool(bool value, MergeType option = MergeType.Min) : base(value, option, false) { }
		public override void Merge(IEnumerable<EventProperty<bool>> values)
		{
			switch (Option)
			{
				case MergeType.Mode: // 평균값/최빈값/중앙값 : 과반수
				case MergeType.Median:
				case MergeType.Average:
					Value = values.Count(v => v.Value) > values.Count() / 2;
					break;
				case MergeType.Max: // 한 개라도 참
					Value = values.Any(v => v.Value);
					break;
				case MergeType.Sum: // bool에서 해당 옵션은 사용하지 않기를 권장
				case MergeType.Min: // 한 개라도 거짓
					Value = values.All(v => v.Value);
					break;
			}
		}
	}
	[Serializable]
	public class EInt : EventProperty<int>
	{
		public EInt(int value, MergeType option = MergeType.Average, bool allowDefault = false) : base(value, option, allowDefault) { }
		public override void Merge(IEnumerable<EventProperty<int>> values)
		{
			switch (Option)
			{
				case MergeType.Sum:
					Value =
						!AllowDefault && values.Any(v => v.Value == default)
						? default
						: values.Sum(v => v.Value);
					break;
				case MergeType.Average:
					Value =
						!AllowDefault && values.Any(v => v.Value == default)
						? default
						: values.Average(v => v.Value).RoundToInt();
					break;
				case MergeType.Max:
					Value = values.Max(v => v.Value);
					break;
				case MergeType.Min:
					Value = values.Min(v => v.Value);
					break;
				case MergeType.Mode:
					Value = values.GroupBy(v => v.Value).OrderByDescending(g => g.Count()).First().Key;
					break;
				case MergeType.Median:
					Value = values.OrderBy(v => v.Value).ElementAt(values.Count() / 2).Value;
					break;
			}
		}
	}
	[Serializable]
	public class EFloat : EventProperty<float>
	{
		public EFloat(int value = default, MergeType option = MergeType.Average, bool allowDefault = false) : base(value, option, allowDefault) { }
		public override void Merge(IEnumerable<EventProperty<float>> values)
		{
			switch (Option)
			{
				case MergeType.Sum:
					Value =
						!AllowDefault && values.Any(v => v.Value == default)
						? default
						: values.Sum(v => v.Value);
					break;
				case MergeType.Average:
					Value =
						!AllowDefault && values.Any(v => v.Value == default)
						? default
						: values.Average(v => v.Value);
					break;
				case MergeType.Max:
					Value = values.Max(v => v.Value);
					break;
				case MergeType.Min:
					Value = values.Min(v => v.Value);
					break;
				case MergeType.Median:
					Value = values.OrderBy(v => v.Value).ElementAt(values.Count() / 2).Value;
					break;
					//? 부동소수점은 최빈값이 없다고 가정한다.
			}
		}
	}
	#endregion

	public abstract class EventAttribute<T>
	{

	}
}