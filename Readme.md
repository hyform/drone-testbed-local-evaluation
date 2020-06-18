# Drone Evaluation Code

The purpose of this code is to evaluate the performance of drones using the
configuration grammar string defined for the A-Teams program.

## Overview

The tool is currently implemented using the Unity gaming engine. It is based off the assessment that was originally used within the drone design client. It takes in a configuration string and returns  cost, range, and velocity. These performance parameters are evaluated by first constructing the design with components and arrangement specified by the config string. The "virtual" design is then evaluated by "dropping" it in space and starting the motors. It then goes through a "hover" period where it attempts to stabilize back to the original release point. Once the system has achieved stable hover, it tilts forward 16 degrees (though this could easily be changed or made into a dynamic setting) to start forward flight. The simulation then runs until the drone runs out of stored energy. The distance traveled and the final speed are returned. The trajectory of the flight can also be returned if a visual feedback on the flight dynamics is desired.

The tool is intended to be used without the frontend designer tools, where external tools generate vehicle configuration strings to be evaluated using this code.  

## Installation Instructions

This evaluation code is implemented in Unity. As a result Unity must be installed on your machine. Our development has been completed in Unity version 2018.04.12f1. Clone or download this project. Start Unity and open a new project using this base folder, and then open the scene located in Assets/Projects/designtool folder (drone_designer scene).


### Local standalone build
To make a local build version, change the platform option in Build Settings to match your host platform. For me this is "Windows x86_64". Select an empty build directory and press Build. To execute, using a command prompt, "cd" into the project build directory and enter : your_executable_name -batchmode -nographics -configuration vehicle_grammar_str. This should start the application in the foreground of the terminal shell and not open a graphics window (the windows_build and linux_build folders include an example Unity build with run.bat or run.sh to start the evaluation)(you will need to "source run.sh" on linux).  Successful runs should create a new results.txt file, where the vehicle configuration, range (mi), capacity (lb), cost ($), and velocity (mph) are separated using ';'.

## Citing HyForm™
If you use HyForm in your own research, please cite it using this paper:

B. Song, N.F. Soria Zurita, G. Zhang, G. Stump, C. Balon, S.W. Miller, M. Yukish, J. Cagan, and C. McComb (2020). "Toward Hybrid Teams: A Platform To Understand Human-Computer Collaboration During the Design of Complex Engineered Systems." Proceedings of the Design Society: DESIGN Conference, 1, 1551-1560. [doi:10.1017/dsd.2020.68](https://doi.org/10.1017/dsd.2020.68)

## Funding
This material is based upon work supported by the Defense Advanced Research Projects Agency through cooperative agreement N66001-17-1-4064. Any opinions, findings, and conclusions or recommendations expressed in this repository are those of the contributors and do not necessarily reflect the views of the sponsors.
