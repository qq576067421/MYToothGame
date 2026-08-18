using System.Collections.Generic;

namespace CompanyInternalTools.BoneParserLib
{
    internal static class BoneGestureCatalog
    {
        private static readonly Dictionary<BoneGestureType, BoneGestureDefinition> m_Definitions =
            BuildDefinitions();

        public static bool TryReadDefinition(BoneGestureType gestureType, out BoneGestureDefinition definition)
        {
            return m_Definitions.TryGetValue(gestureType, out definition);
        }

        private static Dictionary<BoneGestureType, BoneGestureDefinition> BuildDefinitions()
        {
            List<BoneGestureDefinition> definitions = new List<BoneGestureDefinition>();
            BoneGestureRecognizerRegistry.CollectDefaultDefinitions(definitions);

            Dictionary<BoneGestureType, BoneGestureDefinition> result =
                new Dictionary<BoneGestureType, BoneGestureDefinition>();
            for (int i = 0; i < definitions.Count; i++)
            {
                BoneGestureDefinition definition = definitions[i];
                if (definition == null || definition.m_GestureType == BoneGestureType.未知)
                {
                    continue;
                }

                result[definition.m_GestureType] = definition;
            }

            return result;
        }
    }
}
