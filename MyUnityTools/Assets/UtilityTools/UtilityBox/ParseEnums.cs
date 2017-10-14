
namespace UtilityTools {
	public static partial class GeneralTool {
		public static T ParseEnum<T>(string aText) {
			return (T)System.Enum.Parse(typeof(T), aText);
		}
	}
}