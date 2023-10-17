## Notes
1) `.NET 7 SDK` is required to run the application
2) Change the paths in `appsettings.json`
    - `MatFileLocation` - Matlab file to load
    - `ImageSaveLocation` - folder where some processed images are saved for debugging purposes
3) Since the library I'm using tries to load the whole `.mat` file at once, I've created a trimmed version (placed in `Matlab` folder) to speed up the process & reduce memory usage. It also works well with larger Matlab files
4) I used OpenCV's CcoeffNorm template matching algorithm. It tries to get the best match with fitness above the threshold. The threshold can be set in `appsettings.json`
5) `TemplateArea` specifies which ROI in the source image is used as template
6) I mainly tried to match amplitudes, but phases also seem to match (not as well as amplitudes)