using System;
using System.Collections.Generic;
using System.Text;

namespace MrPing.Utility {
	/// <summary>
	/// A simple extension of a list that must store disposable objects. However, this can automatically be disposed of,
	/// cleaning up anything inside it.
	/// </summary>
	class DisposableList : List<IDisposable>, IDisposable {
		/// <summary>
		/// Disposes the list and every element inside of it.
		/// </summary>
		public void Dispose() {
			foreach (var element in this) {
				element?.Dispose();
			}
		}

		/// <summary>
		/// Adds the element to the disposable list, and also returns it. This is convenient if you want to construct
		/// an object and store it in a variable in the same statement as adding it to the disposable list.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="item"></param>
		/// <returns></returns>
		public T AddAndReturn<T>(T item) where T: IDisposable {
			Add(item);
			return item;
		}
	}
}
