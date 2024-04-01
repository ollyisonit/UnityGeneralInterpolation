using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace ollyisonit.UnityGeneralInterpolation
{
	/// <summary>
	/// Contains methods for creating coroutines that can interpolate between any two values.
	/// </summary>
	public static class Interpolator
	{
		/// <summary>
		/// Gets coroutine that will interpolate between given values.
		/// </summary>
		/// <typeparam name="T">Type of object to be interpolated.</typeparam>
		/// <param name="start">Object's starting value.</param>
		/// <param name="end">Object's ending value.</param>
		/// <param name="time">The amount of time the interpolation should take.</param>
		/// <param name="curve">Animation curve from (0, 0) to (1, 1) that defines any easing on the interpolation.</param>
		/// <param name="Output">Method that the current interpolated should be passed to each frame.</param>
		/// <param name="Add">Method to add two values of type T together</param>
		/// <param name="Subtract">Method to Subtract a value of type T</param>
		/// <param name="Scale">Method to scale a value of type T by a float</param>
		/// <returns>A coroutine that will perform the requested interpolation.</returns>
		public static IEnumerator Interpolate<T>(T start, T end, float time, AnimationCurve curve, Action<T> Output,
			Func<T, T, T> Add, Func<T, T, T> Subtract,
			Func<T, float, T> Scale)
		{
			float t = 0;
			while (t < time)
			{
				float scaledT = curve.Evaluate(t / time);
				Output(Interpolate(start, end, scaledT, Add, Subtract, Scale));
				yield return null;
				t += Time.deltaTime;
			}
			Output(Interpolate(start, end, curve.Evaluate(1), Add, Subtract, Scale));
		}

		/// <summary>
		/// Linearly interpolates between start and end values over given amount of time.
		/// </summary>
		/// <typeparam name="T">Type of object to interpolate.</typeparam>
		/// <param name="start">Interpolation starting value.</param>
		/// <param name="end">Interpolation ending value.</param>
		/// <param name="time">Amount of time interpolation should take.</param>
		/// <param name="output">Method to pass current interpolated value to each frame.</param>
		/// <returns>A coroutine that will execute the interpolation.</returns>
		public static IEnumerator Interpolate<T>(T start, T end, float time, Action<T> output)
		{
			return Interpolate(start, end, time, AnimationCurve.Linear(0, 0, 1, 1), output, GetAdd<T>(),
				GetSubtract<T>(), GetScale<T>());
		}


		/// <summary>
		/// Interpolates between start and end values over given amount of time using an AnimationCurve for easing.
		/// </summary>
		/// <typeparam name="T">Type of object to interpolate.</typeparam>
		/// <param name="start">Interpolation starting value.</param>
		/// <param name="end">Interpolation ending value.</param>
		/// <param name="time">Amount of time interpolation should take.</param>
		/// <param name="curve">Animation curve from (0, 0) to (1, 1) that defines easing behavior.</param>
		/// <param name="output">Method to pass current interpolated value to each frame.</param>
		/// <returns>A coroutine that will execute the interpolation.</returns>
		public static IEnumerator Interpolate<T>(T start, T end, float time, AnimationCurve curve, Action<T> output)
		{
			return Interpolate(start, end, time, curve, output, GetAdd<T>(), GetSubtract<T>(), GetScale<T>());
		}


		/// <summary>
		/// Linearly between start and end values with respect to t.
		/// </summary>
		/// <typeparam name="T">Type of object to interpolate.</typeparam>
		/// <param name="start">Interpolation starting value.</param>
		/// <param name="end">Interpolation ending value.</param>
		/// <param name="t">Number from 0.0 to 1.0, where 0.0 corresponds to the starting value and 1.0 corresponds to the ending value.</param>
		/// <param name="Add">Method for adding two values of type T together.</param>
		/// <param name="Subtract">Method for negating a value of type T.</param>
		/// <param name="Scale">Method for scaling a value of type T using a float.</param>
		/// <returns>Interpolated value.</returns>
		public static T Interpolate<T>(T start, T end, float t, Func<T, T, T> Add, Func<T, T, T> Subtract,
			Func<T, float, T> Scale)
		{
			T m = Subtract(end, start);
			return Add(Scale(m, t), start);
		}


		/// <summary>
		///  Linearly interpolates between start and end values of type T using T's implementation of addition, multiplication, and negation.
		/// </summary>
		/// <typeparam name="T">Type of object to interpolate.</typeparam>
		/// <param name="start">Interpolation starting value.</param>
		/// <param name="end">Interpolation ending value.</param>
		/// <param name="t">Number from 0.0 to 1.0, where 0.0 corresponds to the starting value and 1.0 corresponds to the ending value.</param>
		/// <returns>Interpolated value.</returns>
		public static T Interpolate<T>(T start, T end, float t)
		{
			return Interpolate(start, end, t,
				GetAdd<T>(), GetSubtract<T>(), GetScale<T>());
		}


		/// <summary>
		/// Interpolates between start and end values of type T using T's implementation of addition, multiplication, and negation. 
		/// Uses animation curve to define easing.
		/// </summary>
		/// <typeparam name="T">Type of value to interpolate</typeparam>
		/// <param name="start">Interpolation starting value.</param>
		/// <param name="end">Interpolation ending value.</param>
		/// <param name="curve">Animation curve from (0, 0) to (1, 1) that defines easing behavior.</param>
		/// <param name="t">Number from 0.0 to 1.0, where 0.0 corresponds to the starting value and 1.0 corresponds to the ending value.</param>
		/// <returns>Interpolated value.</returns>
		public static T Interpolate<T>(T start, T end, AnimationCurve curve, float t)
		{
			return Interpolate(start, end, curve.Evaluate(t), GetAdd<T>(), GetSubtract<T>(), GetScale<T>());
		}


		/// <summary>
		/// Gets Func version of addition operator for type.
		/// </summary>
		public static Func<T, T, T> GetAdd<T>()
		{
			MethodInfo add = typeof(T).GetMethod("op_Addition", new Type[] { typeof(T), typeof(T) });
			if (add != null)
			{
				return (T a, T b) => (T)add.Invoke(a, new object[] { a, b });
			}

			throw new MissingMethodException("No addition operator found on " + typeof(T));
		}


		/// <summary>
		/// Gets func version of negation operator for type. If no subtraction operator is found, will fall back to
		/// using addition and negation.
		/// </summary>
		public static Func<T, T, T> GetSubtract<T>()
		{
			MethodInfo subtract = typeof(T).GetMethod("op_Subtraction", new Type[] { typeof(T), typeof(T) });
			if (subtract != null)
			{
				return (T a, T b) => (T)subtract.Invoke(a, new object[] { a, b });
			}
			MethodInfo negate = typeof(T).GetMethod("op_UnaryNegation", new Type[] { });
			if (negate != null)
			{
				return (T a, T b) => GetAdd<T>()(a, (T)negate.Invoke(b, new object[] { }));
			}

			throw new MissingMethodException("No subtraction or negation operator found on " + typeof(T));
		}


		/// <summary>
		/// Gets func that uses multiplication operator to multiply the given type by a float.
		/// </summary>
		public static Func<T, float, T> GetScale<T>()
		{
			MethodInfo scale = typeof(T).GetMethod("op_Multiply", new Type[] { typeof(T), typeof(float) });
			if (scale != null)
			{
				return (T a, float b) => (T)scale.Invoke(a, new object[] { a, b });
			}

			throw new MissingMethodException("No multiplication operator between " + typeof(T) + " and float found on " + typeof(T));
		}


		/// <summary>
		/// Returns an object that can be used to build a custom interpolator with user-defined addition, negation, and scaling methods.
		/// </summary>
		public static InterpolatorBuilder<T> GetBuilder<T>()
		{
			return new InterpolatorBuilder<T>();
		}


		/// <summary>
		/// Performs an interpolation with default values that can be easily overriden.
		/// </summary>
		public class InterpolatorBuilder<T>
		{
			public Func<T, T, T> add;
			public Func<T, T, T> Subtract;
			public Func<T, float, T> scale;
			public AnimationCurve curve;


			/// <summary>
			/// Creates an interpolator builder that defaults to using the built-in operators for addition, negation, and scale,
			/// and uses a linear curve for easing.
			/// </summary>
			public InterpolatorBuilder()
			{
				add = GetAdd<T>();
				Subtract = GetSubtract<T>();
				scale = GetScale<T>();
				curve = AnimationCurve.Linear(0, 0, 1, 1);
			}


			/// <summary>
			/// Set the Add function used by this interpolator.
			/// </summary>
			/// <returns>This interpolator with the add function set.</returns>
			public InterpolatorBuilder<T> SetAdd(Func<T, T, T> add)
			{
				this.add = add;
				return this;
			}


			/// <summary>
			/// Set the Negation function used by this interpolator.
			/// </summary>
			/// <returns>This interpolator with the Negation function set.</returns>
			public InterpolatorBuilder<T> SetSubtract(Func<T, T, T> Subtract)
			{
				this.Subtract = Subtract;
				return this;
			}

			/// <summary>
			/// Set the Scale function used by this interpolator.
			/// </summary>
			/// <returns>This interpolator with the Scale function set.</returns>
			public InterpolatorBuilder<T> SetScale(Func<T, float, T> scale)
			{
				this.scale = scale;
				return this;
			}


			/// <summary>
			/// Set the animation curve used by this interpolator.
			/// </summary>
			/// <returns>This interpolator with the animation curve set.</returns>
			public InterpolatorBuilder<T> SetCurve(AnimationCurve curve)
			{
				this.curve = curve;
				return this;
			}


			/// <summary>
			/// Linearly interpolates between start and end values over given amount of time.
			/// </summary>
			/// <typeparam name="T">Type of object to interpolate.</typeparam>
			/// <param name="start">Interpolation starting value.</param>
			/// <param name="end">Interpolation ending value.</param>
			/// <param name="time">Amount of time interpolation should take.</param>
			/// <param name="output">Method to pass current interpolated value to each frame.</param>
			/// <returns>A coroutine that will execute the interpolation.</returns>
			public IEnumerator Interpolate(T start, T end, float time, Action<T> output)
			{
				return Interpolator.Interpolate(start, end, time, curve, output, add, Subtract, scale);
			}


			/// <summary>
			///  Linearly interpolates between start and end values of type T using T's implementation of addition, multiplication, and negation.
			/// </summary>
			/// <typeparam name="T">Type of object to interpolate.</typeparam>
			/// <param name="start">Interpolation starting value.</param>
			/// <param name="end">Interpolation ending value.</param>
			/// <param name="t">Number from 0.0 to 1.0, where 0.0 corresponds to the starting value and 1.0 corresponds to the ending value.</param>
			/// <returns>Interpolated value.</returns>
			public T Interpolate(T start, T end, float t)
			{
				return Interpolator.Interpolate(start, end, curve.Evaluate(t), add, Subtract, scale);
			}
		}
	}
}
