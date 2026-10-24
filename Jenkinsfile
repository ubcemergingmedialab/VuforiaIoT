// Jenkinsfile template for Unity projects that must build on the Windows agent (no Build Profiles).
// Use it for projects the Linux agent can't build, for example ones that use the Vuforia built
// into Unity 2017.2 to 2019.2, or Wwise. Everything else should use the Linux templates.
// Builds the scenes enabled in File > Build Settings for each TARGET with
// Assets/Editor/CommandLineBuild.cs. See "Configuring a Legacy Unity Build Pipeline.docx".
//
// Windows agent notes:
// - Editors are at C:/Unity/Hub/Editor/<version>/Editor/Unity.exe. The version must be in
//   UNITY_INSTALLS in windows/Dockerfile of jenkins-images, with the modules your targets need.
//   Win64 (Mono) needs no module; IL2CPP needs windows-il2cpp from Unity 2019 on.
// - Win64 IL2CPP compiles with the Visual Studio Build Tools in the image. Unity versions older
//   than the Build Tools may not find them; switch the project to Mono if IL2CPP fails.
// - Unity writes its log to a file on Windows, so the Build step prints that file as it grows.
//
// VuforiaIoT: builds on the Windows agent because it uses the Vuforia 7.5 built into
// Unity 2018.2, which has no Linux editor module and no Linux or WebGL player. The agent
// image needs UNITY_INSTALLS entry 2018.2.17f1:88933597c842:vuforia-ar (Win64 Mono needs
// no extra module). The CogsRobot submodule is the robot's firmware, not part of the
// Unity project, so the build doesn't fetch it.
def buildDir(String target, String buildType) {
  def projectPrefix = (env.PROJECT_DIR == '.') ? '' : "${env.PROJECT_DIR}/"
  return "${projectPrefix}Builds/${target}/${buildType}"
}

pipeline {
  agent none
  environment {
    // Unity version of the project (ProjectSettings/ProjectVersion.txt)
    UNITY_VERSION = '2018.2.17f1'
    // Folder of the Unity project (the one with Assets and ProjectSettings),
    // relative to the repository root. '.' when the project is at the root.
    PROJECT_DIR = '.'
  }
  options {
    // Declarative otherwise checks out on every stage that allocates an agent,
    // which would sync the project twice in each build cell. Checkout is done
    // explicitly in the Checkout stage instead.
    skipDefaultCheckout()

    // Keep archived artifacts only for the most recent build (Jenkins also always
    // keeps the last successful build's artifacts), but keep the build records,
    // including their console logs with the full Unity output, for the last 25 builds.
    buildDiscarder(logRotator(numToKeepStr: '25', artifactNumToKeepStr: '1'))
  }
  stages {
    stage("Build and test") {
      failFast true
      matrix {
        axes {
          axis {
            // Unity -buildTarget names supported by CommandLineBuild.cs
            name 'TARGET'
            values 'Win64'
          }
          axis {
            // Add 'Development' to also build development players
            name 'BUILD_TYPE'
            values 'Release'
          }
          axis {
            name 'OS'
            values 'windows'
          }
        }
        // No agent at the matrix level: a matrix cell doesn't claim an
        // executor until the stage below has acquired the license lock.
        stages {
          stage("Licensed build") {
            options {
              // Stage-level options are applied BEFORE the stage's agent is
              // allocated, so cells queue for the lock on the controller
              // without holding an executor.
              lock('unity-license')
            }
            agent {
              label "${OS}"
            }
            stages {
              stage("Checkout") {
                steps {
                  checkout scm
                }
              }
              stage("Build") {
                steps {
                  // Start from an empty output folder so files from earlier
                  // builds can't end up archived.
                  powershell "Remove-Item -Force -ErrorAction SilentlyContinue 'build-${TARGET}_${BUILD_TYPE}.log'"
                  powershell "Remove-Item -Recurse -Force -ErrorAction SilentlyContinue '${buildDir(TARGET, BUILD_TYPE)}'"

                  withCredentials([
                    string(
                      credentialsId: 'unity_license_serial',
                      variable: 'UNITY_LICENSE_SERIAL'
                    ),
                    usernamePassword(
                      credentialsId: 'unity_license_user',
                      usernameVariable: 'UNITY_USER',
                      passwordVariable: 'UNITY_PASSWORD'
                    )
                  ]) {
                    powershell '''
                      $ErrorActionPreference = 'Stop'
                      $unity = "C:/Unity/Hub/Editor/$env:UNITY_VERSION/Editor/Unity.exe"
                      if (-not (Test-Path $unity)) {
                        throw "Unity $env:UNITY_VERSION is not installed on this agent. Ask an admin to add it to the Windows worker image."
                      }
                      $log = Join-Path $env:WORKSPACE "build-$($env:TARGET)_$($env:BUILD_TYPE).log"
                      # -projectPath must be absolute: Unity 2017 rejects "-projectPath .".
                      $project = (Resolve-Path (Join-Path $env:WORKSPACE $env:PROJECT_DIR)).Path

                      $unityArgs = @(
                        '-username', $env:UNITY_USER,
                        '-password', $env:UNITY_PASSWORD,
                        '-serial', $env:UNITY_LICENSE_SERIAL,
                        '-batchmode',
                        '-nographics',
                        '-quit',
                        '-projectPath', $project,
                        '-buildTarget', $env:TARGET,
                        '-buildType', $env:BUILD_TYPE,
                        '-executeMethod', 'CommandLineBuild.Build',
                        '-logFile', $log
                      )
                      if ($env:BUILD_TYPE -eq 'Development') { $unityArgs += '-devBuild' }
                      # Quote each argument for the Windows command line, so passwords and
                      # paths with spaces or quotes reach Unity unchanged.
                      $argLine = ($unityArgs | ForEach-Object {
                        '"' + (($_ -replace '(\\\\*)"', '$1$1\\"') -replace '(\\\\+)$', '$1$1') + '"'
                      }) -join ' '

                      $psi = New-Object System.Diagnostics.ProcessStartInfo
                      $psi.FileName = $unity
                      $psi.Arguments = $argLine
                      $psi.UseShellExecute = $false
                      $p = [System.Diagnostics.Process]::Start($psi)

                      # Print the Unity log to the console as it grows, until Unity exits.
                      $pos = 0
                      while ($true) {
                        $exited = $p.HasExited
                        if (Test-Path $log) {
                          $fs = [System.IO.File]::Open($log, 'Open', 'Read', 'ReadWrite')
                          try {
                            [void]$fs.Seek($pos, 'Begin')
                            $text = (New-Object System.IO.StreamReader($fs)).ReadToEnd()
                            $pos = $fs.Position
                          } finally {
                            $fs.Dispose()
                          }
                          if ($text) { Write-Host -NoNewline $text }
                        }
                        if ($exited) { break }
                        Start-Sleep -Seconds 2
                      }
                      exit $p.ExitCode
                    '''
                  }
                }
                post {
                  always {
                    archiveArtifacts artifacts: "build-${TARGET}_${BUILD_TYPE}.log", allowEmptyArchive: true, fingerprint: true
                  }
                }
              }
              stage("Archive") {
                steps {
                  archiveArtifacts artifacts: "${buildDir(TARGET, BUILD_TYPE)}/**/*", allowEmptyArchive: false, fingerprint: true
                }
              }
            }
          }
        }
      }
    }
  }
}
