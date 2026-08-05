using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.PackageManager;

namespace LGU
{
    [InitializeOnLoad]
    public class RenderingPipelineDefines
    {

        // URP_13_OR_NEWER
        // URP_12_OR_NEWER

        // UNITY_PIPELINE_BUILD_IN
        // UNITY_PIPELINE_URP
        // UNITY_PIPELINE_HDRP
        enum PipelineType
        {
            Unsupported,
            BuiltInPipeline,
            UniversalPipeline,
            HDPipeline
        }

        static RenderingPipelineDefines()
        {
            UpdateDefines();
            UpdateUrpVersion();
        }

        /// <summary>
        /// Update the unity pipeline defines for URP
        /// </summary>
        static void UpdateDefines()
        {
            var pipeline = GetPipeline();

            if (pipeline == PipelineType.UniversalPipeline)
            {
                DefinesUtils.AddDefine("UNITY_PIPELINE_URP");
            }
            else
            {
                DefinesUtils.RemoveDefine("UNITY_PIPELINE_URP");
            }
            if (pipeline == PipelineType.HDPipeline)
            {
                DefinesUtils.AddDefine("UNITY_PIPELINE_HDRP");
            }
            else
            {
                DefinesUtils.RemoveDefine("UNITY_PIPELINE_HDRP");
            }
            if (pipeline == PipelineType.BuiltInPipeline)
            {
                DefinesUtils.AddDefine("UNITY_PIPELINE_BUILD_IN");
            }
            else
            {
                DefinesUtils.RemoveDefine("UNITY_PIPELINE_BUILD_IN");
            }

        }


        /// <summary>
        /// Returns the type of renderpipeline that is currently running
        /// </summary>
        /// <returns></returns>
        static PipelineType GetPipeline()
        {
#if UNITY_2019_1_OR_NEWER
            if (GraphicsSettings.defaultRenderPipeline != null)
            {
                // SRP
                var srpType = GraphicsSettings.defaultRenderPipeline.GetType().ToString();
                if (srpType.Contains("HDRenderPipelineAsset"))
                {
                    return PipelineType.HDPipeline;
                }
                else if (srpType.Contains("UniversalRenderPipelineAsset") || srpType.Contains("LightweightRenderPipelineAsset"))
                {
                    return PipelineType.UniversalPipeline;
                }
                else return PipelineType.Unsupported;
            }
#elif UNITY_2017_1_OR_NEWER
        if (GraphicsSettings.renderPipelineAsset != null) {
            // SRP not supported before 2019
            return PipelineType.Unsupported;
        }
#endif
            // no SRP
            return PipelineType.BuiltInPipeline;
        }

        static UnityEditor.PackageManager.Requests.ListRequest packageListRequest;
        static void UpdateUrpVersion()
        {
            packageListRequest = Client.List();
            EditorApplication.update += OnPackageListRequest;
        }

        static void OnPackageListRequest()
        {
            if (packageListRequest.IsCompleted)
            {
                if (packageListRequest.Status == StatusCode.Success)
                {

                    foreach (var package in packageListRequest.Result)
                    {
                        if (package.name == "com.unity.render-pipelines.universal")
                        {
                            string version = package.version.Split(".")[0];
                            if (int.Parse(version) >= 13)
                            {
                                DefinesUtils.AddDefine("URP_13_OR_NEWER");
                                DefinesUtils.RemoveDefine("URP_12_OR_NEWER");
                            }
                            else
                            {
                                DefinesUtils.AddDefine("URP_12_OR_NEWER");
                                DefinesUtils.RemoveDefine("URP_13_OR_NEWER");
                            }

                        }
                    }
                }

                EditorApplication.update -= OnPackageListRequest;
            }
        }
    }
}