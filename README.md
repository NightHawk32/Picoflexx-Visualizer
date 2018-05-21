## Picoflexx Visualizer
This is a small demo application, which uses libroyale in C# to collect image information from a Picoflexx PMD-Sensor. 
It also includes a WFP-GUI, which displays the image information and allows to save the images to disk. The GUI shows the available PMD cameras 
and allows to switch between different operation modes.

![alt text](https://github.com/NightHawk32/Picoflexx-Visualizer/blob/master/doc/wpf_gui.PNG "Picoflexx WPF GUI")

## H1 Build Instructions
Copy the following files to the picoflexx_visualizer folder:
* royale.dll
* royaleCAPI.dll
* RoyaleDotNet.dll
You also need to match the build architecture (32bit or 64bit) in visual studio to your libroyale version.
