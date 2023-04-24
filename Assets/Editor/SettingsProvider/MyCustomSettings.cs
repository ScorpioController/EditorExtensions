using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.SettingsProvider
{
    // 创建自定义设置所对应的资源类型
    class MyCustomSettings : ScriptableObject
    {
        public const string k_MyCustomSettingsPath = "Assets/Editor/SettingsProvider/MyCustomSettings.asset";

        [SerializeField]
        private int m_Number;

        [SerializeField]
        private string m_SomeString;

        internal static MyCustomSettings GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MyCustomSettings>(k_MyCustomSettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<MyCustomSettings>();
                settings.m_Number = 42;
                settings.m_SomeString = "The answer to the universe";
                AssetDatabase.CreateAsset(settings, k_MyCustomSettingsPath);
                AssetDatabase.SaveAssets();
            }
            return settings;
        }

        internal static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(GetOrCreateSettings());
        }
    }
    
    // 继承SettingsProvider以实现自定义的SettingsProvider:
    class MyCustomSettingsProvider : UnityEditor.SettingsProvider
    {
        private SerializedObject m_CustomSettings;

        class Styles
        {
            public static GUIContent number = new GUIContent("My Number");
            public static GUIContent someString = new GUIContent("Some string");
        }
        public MyCustomSettingsProvider(string path, SettingsScope scope = SettingsScope.User)
            : base(path, scope) {}

        // 检查设置项是否存在
        public static bool IsSettingsAvailable()
        {
            return File.Exists(MyCustomSettings.k_MyCustomSettingsPath);
        }

        // 当用户在设置面板中点击自定义元素时调用该方法
        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            m_CustomSettings = MyCustomSettings.GetSerializedSettings();
        }

        public override void OnGUI(string searchContext)
        {
            // Use IMGUI to display UI:
            EditorGUILayout.PropertyField(m_CustomSettings.FindProperty("m_Number"), Styles.number);
            EditorGUILayout.PropertyField(m_CustomSettings.FindProperty("m_SomeString"), Styles.someString);
            m_CustomSettings.ApplyModifiedPropertiesWithoutUndo();
        }

        // 注册SettingProvider
        [SettingsProvider]
        public static UnityEditor.SettingsProvider CreateMyCustomSettingsProvider()
        {
            if (IsSettingsAvailable())
            {
                // 第一个参数决定了设置页在设置窗口中的位置，第二个参数决定设置页出现在 Project Setting Window 还是 Preference Window 中
                var provider = new MyCustomSettingsProvider("Project/MyCustomSettingsProvider", SettingsScope.Project);

                // 自动提取样式中的所有关键字
                provider.keywords = GetSearchKeywordsFromGUIContentProperties<Styles>();
                return provider;
            }

            // 设置资源不存在时不需要显示任何内容
            return null;
        }
    }

    // 使用IMGUI注册SettingsProvider:
    static class MyCustomSettingsIMGUIRegister
    {
        [SettingsProvider]
        public static UnityEditor.SettingsProvider CreateMyCustomSettingsProvider()
        {
            var provider = new UnityEditor.SettingsProvider("Project/MyCustomIMGUISettings", SettingsScope.Project)
            {
                // 在没有设置label属性的默认情况下，路径中的最后一部分将会作为设置项的名称
                label = "Custom IMGUI",
                // 创建SettingsProvider然后初始化SettingsProvider的绘制方法
                guiHandler = (searchContext) =>
                {
                    var settings = MyCustomSettings.GetSerializedSettings();
                    EditorGUILayout.PropertyField(settings.FindProperty("m_Number"), new GUIContent("My Number"));
                    EditorGUILayout.PropertyField(settings.FindProperty("m_SomeString"), new GUIContent("My String"));
                    settings.ApplyModifiedPropertiesWithoutUndo();
                },

                // 填充搜索关键字以支持搜索过滤和标签高亮
                keywords = new HashSet<string>(new[] { "Number", "Some String" })
            };

            return provider;
        }
    }

    // 使用UIElements注册SettingsProvider:
    static class MyCustomSettingsUIElementsRegister
    {
        [SettingsProvider]
        public static UnityEditor.SettingsProvider CreateMyCustomSettingsProvider()
        {
            var provider = new UnityEditor.SettingsProvider("Project/MyCustomUIElementsSettings", SettingsScope.User)
            {
                label = "Custom UI Elements",
                // 当用户在设置面板中点击自定义元素时调用该事件
                activateHandler = (searchContext, rootElement) =>
                {
                    var settings = MyCustomSettings.GetSerializedSettings();
                    
                    var title = new Label()
                    {
                        text = "Custom UI Elements"
                    };
                    title.AddToClassList("title");
                    rootElement.Add(title);

                    var properties = new VisualElement()
                    {
                        style =
                        {
                            flexDirection = FlexDirection.Column
                        }
                    };
                    properties.AddToClassList("property-list");
                    rootElement.Add(properties);

                    properties.Add(new PropertyField(settings.FindProperty("m_SomeString")));
                    properties.Add(new PropertyField(settings.FindProperty("m_Number")));

                    rootElement.Bind(settings);
                },
                
                // 填充搜索关键字以支持搜索过滤和标签高亮
                keywords = new HashSet<string>(new[] { "Number", "Some String" })
            };

            return provider;
        }
    }
}