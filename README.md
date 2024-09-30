# Supervisory Control And Data Acquisition Project (SCADA)
## Description
Here is the repository containing the implementation of a SCADA platform for a custom-type plant.

## Requirements
- FactoryTalk® Optix™ version 1.4.2.3
- SSMS version 20
- Visual Studio Community 2022

### Task provided by the Rea_Robotics_S.R.L team for my internship training

## How to clone the repository
#### Cloning in FactoryTalk® Optix™ (preferred)
1. Click on the green `CODE` button in the top right corner
2.  Select `HTTPS` and copy the provided URL
3.  Open FactoryTalk® Optix™ IDE
4.  Click on `Open` and select the `Remote` tab
5.  Paste the HTTPS URL from step 2
6.  Click `Open` button in the bottom right corner to start the cloning process

#### Downloading ZIP file
1. Click on the green `CODE` button in the top right corner
2. Click on the `Download ZIP` button

#### First Use & Features
First of all, connect to the SQLExpress database if you want to use the online DB. Secondly, you can create two folders: one for the InserimentoAnagraficaSuDB function, and insert the path into the `RunTimeNetLogic1` script if you want to use the feature for file insertion, and another for the InserimentoProduzioneSuDB function, again inserting the path into the 'RunTimeNetLogic2' script. Once this is completed, the program will be ready, and if you want to modify or import your own local DB, simply insert it as the `DB_Locale.sqlite` file in the folder `...\AppData\Local\Rockwell Automation\FactoryTalk Optix\Emulator\Projects\SCADA\ApplicationFiles` when the project doesn't running

## Software Used
1. FactoryTalk® Optix™ by Rockwell Automation
   - combines cloud-based software to create innovative projects and scale them through Responsive Design techniques, allowing use even on physically remote
     systems; and flexible hardware options, including dedicated panels and industrial PCs, to support a wide range of applications, creating end-to-end solutions
     for operator interfaces and the Industrial Internet of Things that offer flexibility throughout the lifecycle of machines.
2. SQL Server Management Studio (SSMS)
   - is an integrated environment for managing any SQL infrastructure. It was used for accessing, configuring, managing, administering, and developing all SQL
     Server components. SSMS is a comprehensive utility that integrates a wide array of graphical tools with numerous advanced script editors to provide SQL
     Server access for developers and database administrators at any skill level.
3. Visual Studio Community 2022
   - is an integrated development environment (IDE) developed by Microsoft. It allows the development of programs through programming languages using a single
     workspace environment.
