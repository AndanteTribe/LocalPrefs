using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AndanteTribe.IO.Unity.Tests
{
    public class LSPrefsTest
    {
        [Test]
        public void LSPrefsTestSimplePasses()
        {
            // Use the Assert class to test conditions.
            
        }

        // A UnityTest behaves like a coroutine in PlayMode
        // and allows you to yield null to skip a frame in EditMode
        [UnityTest]
        public IEnumerator LSPrefsTestWithEnumeratorPasses()
        {
            // Use the Assert class to test conditions.
            // yield to skip a frame
            yield return null;
        }
    }
}