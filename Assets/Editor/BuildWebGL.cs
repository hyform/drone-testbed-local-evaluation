using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BuildWebGL {
	static void Build() {
		string[] scenes = {
            "Assets/Projects/designtool/drone_designer.unity",
            "Assets/Projects/plantool/plan_tool.unity"
        };
		BuildPlayerOptions buildOptions = new BuildPlayerOptions();
		buildOptions.scenes = scenes;
		buildOptions.target = BuildTarget.WebGL;
		buildOptions.options = BuildOptions.None;

		string[] args = System.Environment.GetCommandLineArgs();
		string buildPath = "";
		for(int i = 0; i < args.Length; i++) {
			if (args[i] == "-buildPath" && i < args.Length - 1) {
				buildPath = args [i + 1];
			}
		}
		buildOptions.locationPathName = buildPath;

		BuildPipeline.BuildPlayer(buildOptions);
	}
}
