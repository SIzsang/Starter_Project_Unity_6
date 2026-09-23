using System.Collections;
using NUnit.Framework;
using StarterProject.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace StarterProject.Tests
{
    public sealed class StarterCanvasLayoutTests
    {
        [UnityTest]
        public IEnumerator LandscapeCanvasFitsInteractiveContentInsideSafeArea()
        {
            var root = new GameObject("Layout Test", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var background = CreateRect(root.transform, "Background", true);
            var button = CreateRect(root.transform, "Action", false);
            var canvas = root.GetComponent<Canvas>();
            var layout = StarterCanvasLayout.EnsureConfigured(canvas);

            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(root.GetComponent<CanvasScaler>().uiScaleMode,
                Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
            Assert.That(background.parent, Is.EqualTo(root.transform), "The background must cover the cutout area.");
            Assert.That(button.parent, Is.EqualTo(layout.ContentRoot), "Controls must stay inside the safe area.");

            // 2400x1080 가로 화면, 양쪽 80px 노치 여백, Canvas scale factor 2.
            layout.ApplyLayout(2400, 1080, new Rect(80, 0, 2240, 1080), 2f);
            Assert.That(layout.SafeAreaRoot.anchorMin.x, Is.EqualTo(80f / 2400f).Within(0.0001f));
            Assert.That(layout.SafeAreaRoot.anchorMax.x, Is.EqualTo(2320f / 2400f).Within(0.0001f));
            Assert.That(layout.ContentRoot.localScale.x, Is.EqualTo(0.75f).Within(0.0001f));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DesktopResolutionChangeReflowsWithoutDuplicatingHierarchy()
        {
            var root = new GameObject("Layout Test", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var button = CreateRect(root.transform, "Action", false);
            var layout = StarterCanvasLayout.EnsureConfigured(root.GetComponent<Canvas>());
            layout.ApplyLayout(1920, 1080, new Rect(0, 0, 1920, 1080), 1.5f);
            Assert.That(layout.ContentRoot.localScale.x, Is.EqualTo(1f).Within(0.0001f));

            layout.ApplyLayout(960, 540, new Rect(0, 0, 960, 540), 1f);
            Assert.That(layout.ContentRoot.localScale.x, Is.EqualTo(0.75f).Within(0.0001f));
            Assert.That(StarterCanvasLayout.EnsureConfigured(root.GetComponent<Canvas>()), Is.SameAs(layout));
            Assert.That(root.transform.childCount, Is.EqualTo(1));
            Assert.That(button.parent, Is.EqualTo(layout.ContentRoot));

            Object.Destroy(root);
            yield return null;
        }

        private static RectTransform CreateRect(Transform parent, string name, bool fullScreen)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = fullScreen ? Vector2.zero : new Vector2(0.5f, 0.5f);
            rect.anchorMax = fullScreen ? Vector2.one : new Vector2(0.5f, 0.5f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }
    }
}
