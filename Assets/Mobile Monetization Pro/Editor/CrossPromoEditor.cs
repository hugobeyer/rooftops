using UnityEngine;
using UnityEditor;

namespace MobileMonetizationPro
{
    [CustomEditor(typeof(MobileMonetizationPro_CrossPromo))]
    public class CrossPromoEditor : Editor
    {
        SerializedProperty ChooseCrossPromoType;
        SerializedProperty DecideWhenToShowNextPromo;
        SerializedProperty NoOfAppOpensToCheckBeforeNewPromo;
        SerializedProperty NoOfSessionsToCheckBeforeNewPromo;
        SerializedProperty MinTimeToWaitBeforeChangingPromo;
        SerializedProperty MaxTimeToWaitBeforeChangingPromo;

        void OnEnable()
        {
            ChooseCrossPromoType = serializedObject.FindProperty("ChooseCrossPromoType");
            DecideWhenToShowNextPromo = serializedObject.FindProperty("DecideWhenToShowNextPromo");
            NoOfAppOpensToCheckBeforeNewPromo = serializedObject.FindProperty("NoOfAppOpensToCheckBeforeNewPromo");
            NoOfSessionsToCheckBeforeNewPromo = serializedObject.FindProperty("NoOfSessionsToCheckBeforeNewPromo");
            MinTimeToWaitBeforeChangingPromo = serializedObject.FindProperty("MinTimeToWaitBeforeChangingPromo");
            MaxTimeToWaitBeforeChangingPromo = serializedObject.FindProperty("MaxTimeToWaitBeforeChangingPromo");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("ChooseCrossPromoType"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("ChooseDisplayOption"));

            EditorGUILayout.PropertyField(DecideWhenToShowNextPromo);

            MobileMonetizationPro_CrossPromo.OptionsToChangeSprites chosenOption = (MobileMonetizationPro_CrossPromo.OptionsToChangeSprites)DecideWhenToShowNextPromo.enumValueIndex;

            MobileMonetizationPro_CrossPromo.CrossPromotype chosenOptionForPromo = (MobileMonetizationPro_CrossPromo.CrossPromotype)ChooseCrossPromoType.enumValueIndex;

            EditorGUI.indentLevel++;

            switch (chosenOption)
            {
                case MobileMonetizationPro_CrossPromo.OptionsToChangeSprites.BasedOnAppOpens:
                    EditorGUILayout.PropertyField(NoOfAppOpensToCheckBeforeNewPromo);
                    break;
                case MobileMonetizationPro_CrossPromo.OptionsToChangeSprites.BasedOnSession:
                    EditorGUILayout.PropertyField(NoOfSessionsToCheckBeforeNewPromo);
                    break;
                case MobileMonetizationPro_CrossPromo.OptionsToChangeSprites.BasedOnTimer:
                    EditorGUILayout.PropertyField(MinTimeToWaitBeforeChangingPromo);
                    EditorGUILayout.PropertyField(MaxTimeToWaitBeforeChangingPromo);
                    break;
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("StopCrossPromotionAfterClick"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("CrossPromotionToDeactive"));



            switch (chosenOptionForPromo)
            {
                case MobileMonetizationPro_CrossPromo.CrossPromotype.VideoType:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("videoPlayer"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("RawImageComponent"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("RenderTextureComponent"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("AddVideos"));
                    break;
                case MobileMonetizationPro_CrossPromo.CrossPromotype.ImageType:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("ImageComponent"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("AddSprites"));
                    break;
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}