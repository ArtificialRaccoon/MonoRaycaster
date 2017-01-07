using System;

namespace OpenGLTest
{
	//Simple way to modify paramters in a narrow scope (similar to VB's "with" method)
	//http://omaralzabir.com/c__with_keyword_equivalent/
	public static class ExtensionMethods
	{
		public static void Use<T>(this T item, Action<T> work)
		{
			work(item);
		}
	}
}