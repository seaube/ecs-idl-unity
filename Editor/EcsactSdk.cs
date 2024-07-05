using UnityEngine;
using UnityEditor;
using System.Linq;
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace Ecsact.Editor {

public static class EcsactSdk {
	public const string INSTALL_URL =
		"https://github.com/ecsact-dev/ecsact_sdk/releases";

	private static bool shownDialogRecently = false;

	private static string SearchEnvironmentPath(string name) {
		return Environment.GetEnvironmentVariable("PATH")
			.Split(Path.PathSeparator)
			.Select(s => Path.Combine(s, name))
			.Where(path => File.Exists(path))
			.FirstOrDefault();
	}

	public static string FindExecutable(string name) {
#if UNITY_EDITOR_WIN
		if(!name.EndsWith(".exe")) {
			name += ".exe";
		}
#endif

		var executablePath = SearchEnvironmentPath(name);
		if(string.IsNullOrEmpty(executablePath)) {
			// FindExecutable often gets called rapidly in sequence. We do our best
			// to make this dialog not show up immediately after a dialog choice was
			// made.
			if(!shownDialogRecently) {
				shownDialogRecently = true;
				EditorApplication.delayCall += () => { shownDialogRecently = false; };

				var wantsInstall = EditorUtility.DisplayDialog(
					"Ecsact SDK Executable(s) Not Found",
					"The Ecsact SDK is required for the Escact unity integration to " +
						$"work correctly.\n\nMissing SDK Executable: {name}",
					"Install Ecsact SDK",
					"Cancel"
				);

				if(wantsInstall) {
					Application.OpenURL(INSTALL_URL);
				}
			}

			throw new Exception(
				"Ecsact SDK not found. Please install the Ecsact SDK and try again."
			);
		}
		return executablePath;
	}

	public delegate void RecipeCallback(List<string> recipeList);

	public static void GetRecipeBundles(RecipeCallback callback) {
		List<string> recipe_bundles = new();

		string ecsactExecutablePath = EcsactSdk.FindExecutable("ecsact");

		var proc = new Process();
		proc.StartInfo.FileName = ecsactExecutablePath;
		proc.StartInfo.CreateNoWindow = true;
		proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
		proc.StartInfo.Arguments = "config recipe_bundles";
		proc.EnableRaisingEvents = true;
		proc.StartInfo.UseShellExecute = false;
		proc.StartInfo.RedirectStandardOutput = true;

		proc.OutputDataReceived += ((_, e) => {
			if(string.IsNullOrEmpty(e.Data)) return;
			recipe_bundles.Add(e.Data);
		});

		proc.Exited += ((_, e) => { callback(recipe_bundles); });

		proc.Start();
		proc.BeginOutputReadLine();
	}
}

} // namespace Ecsact.Editor
