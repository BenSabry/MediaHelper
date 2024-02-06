# Media Helper
<b>*Media Helper*</b> is a tool engineered to organize your media files into the ***/Library/Year/Month/file*** structure, using their creation date as a guide. It's also useful for retrieving any missing metadata from JSON files (for example, Google Takeout), and correcting metadata issues such as duplications or incorrect offsets. It also aids in resolving any <b>*[Issues like these](#Fixable-Issues)*</b>.

# Downloads
<b>*Latest release*</b> [v0.10.0-beta](https://github.com/BenSabry/MediaHelper/releases/tag/v0.10.0-beta)<br />
<b>*All releases*</b> [releases](https://github.com/BenSabry/MediaHelper/releases)

# Story
I’m a proud owner of a Synology (DS720+) and I must say, I’m quite fond of it, especially the Photos/Drive apps.<br />

Previously, I was a Google Photos user, so I downloaded a complete backup of all my media from Google and transferred it to the Synology Photos folder. However, I encountered a significant issue - Some media files lack certain metadata, and the order of the media. All my media was arranged according to the date I uploaded it to Synology, not the creation date or date taken, etc. After some research, I found numerous online discussions about this issue, even Synology Photos’ solution of manually modifying the image date is good but not feasible for a batch of more than 20,000 files!<br />

So, I decided to tackle this problem head-on and develop my own solution. After several days of development and testing on my own media library, I’m happy to report that it’s complete and all my files are now properly sorted on Synology Photos, complete with the **retrieved metadata** extracted from the JSON files included in my Google Takeout backup.<br />

Why am I sharing this? Because I experienced this frustrating problem and I want to help others who might be facing the same issue. If you’re interested, you can try it out and share your feedback.<br />

Please remember to BACKUP your media before trying anything new, whether it’s from me or anyone else. Lastly, a big thank you to Synology for the excellent combination of software and hardware. I truly enjoy your product.

# Notice
This tool is compatible with <b>*Synology, Qnap*</b>, and other NAS solutions, as well as local media files. If your media is scattered across multiple sources, folders, or locations, and you wish to consolidate and organize them in one place, this tool is ideal. It also supports <b>*Google Takeout*</b> backups and archived zip files. Simply add these to the sources section in <b>*[AppSettings.json](#AppSettings)*</b>.

# Recommendations
a. <b>*BACKUP*</b> your media files first.<br />
b. Increase the <b>*TasksCount*</b> in <b>*[AppSettings.json](#AppSettings)*</b> (recommended: 2, best: <b>*CPU Cores*</b> count)<br />

# How to use
1. Add the path of your library to <b>*Sources*</b> and <b>*Target*</b> in <b>*[AppSettings.json](#AppSettings)*</b> file<br />
2. run the executable <b>*MediaHelper.exe*</b> and wait<br />

# How it works
1. Scan library files and directories added to <b>*Sources*</b> in <b>*[AppSettings.json](#AppSettings)*</b><br />
2. Continue processing from where files were last handled, skipping those already processed.<br />
3. Extract metadata from JSON files if any included (e.g., Google Takeout JSON files).<br />
4. Extract dates from file metadata, JSON files, and filenames, utilizing a variety of supported datetime format patterns, and then select the **earliest** datetime.<br />
5. Copy the file to proper directory <b>*Target\Year\Month\File.*</b> based on oldest datetime.<br />
6. Update the missing metadata using information from the JSON files.<br />
7. Update the creation date tags (e.g., TakenDate, CreateDate) with the earliest valid datetime discovered.<br />
8. Attempt to fix file metadata (like duplications/incorrect offsets etc.)<br />
9. Remove any empty directories or temporary backups that were created.<br /><br />

# AppSettings
Example of working AppSettings.json.
```JSON
{
    "TasksCount": 2,
    "EnableLogAndResume": true,
    "AttemptToFixMediaIncorrectOffsets": true,
    "ClearBackupFilesOnComplete": true,
    "DeleteEmptyDirectoriesOnComplete": true,
    "AutoFixArabicNumbersInFileName": true,
    "Target": "\\\\SynologyNAS\\home\\Photos\\PhotoLibrary",
    "Sources": [
        "\\\\SynologyNAS\\home\\Google-Takeout.zip",
        "\\\\SynologyNAS\\home\\Photos\\MobileBackup",
        "D:\\Data\\Media\\Photos\\Personal\\Family",
        "\\\\SynologyNAS\\home\\GraduationPhoto.jpg",
        "\\\\SynologyNAS\\home\\GraduationVideo.mp4"
    ],
    "Ignores": [
        "PersonalVideos",
        ".avi",
        ".m4a"
    ]
}
```

# Fixable Issues
[<b>*Synology Photos: Not Using Taken Date*</b>](https://www.reddit.com/r/synology/comments/kgy604/synology_photos_not_using_taken_date/)<br />
[<b>*Synology Photos: organizes everything by modified date instead of creation date*</b>](https://www.reddit.com/r/synology/comments/120jsvk/synology_photos_organizes_everything_by_modified/)<br />
[<b>*Synology Photos: Indexing photos/videos with wrong date*</b>](https://www.reddit.com/r/synology/comments/qj9wya/synology_photos_indexing_photosvideos_with_wrong/)<br />
[<b>*Synology Photos: Best practice for photos with no taken date*</b>](https://www.reddit.com/r/synology/comments/rn5cvm/best_practice_for_photos_with_no_taken_date/)<br />

# Tech/Tools used
<b>*[.NET](https://dotnet.microsoft.com/)*</b>: is the free, open-source, cross-platform framework for building modern apps and powerful cloud services.<br />
<b>*[ExifTool](https://exiftool.org/)*</b>: is a customizable set of Perl modules plus a full-featured
command-line application for reading and writing meta information in a wide
variety of files, including the maker note information of many digital
cameras by various manufacturers such as Canon, Casio, DJI, FLIR, FujiFilm,
GE, HP, JVC/Victor, Kodak, Leaf, Minolta/Konica-Minolta, Nikon, Nintendo,
Olympus/Epson, Panasonic/Leica, Pentax/Asahi, Phase One, Reconyx, Ricoh,
Samsung, Sanyo, Sigma/Foveon and Sony.<br />

# AppSettings Explanation
<b>*TasksCount*</b>: (number) of <b>*Tasks/Threads*</b> to work simultaneously.<br />
<b>*EnableLogAndResume*</b>: (flag) to ensure continuity and avoid repetition, actions are recorded in the log.txt files. This allows for resumption from the exact point where you last left off, rather than starting anew.<br />
<b>*AttemptToFixMediaIncorrectOffsets*</b>: (flag) to fix file info (like duplications/incorrect offsets etc.)<br />
<b>*ClearBackupFilesOnComplete*</b>: (flag) Clear temp files on complete.<br />
<b>*DeleteEmptyDirectoriesOnComplete*</b>: (flag) Delete empty directories on complete.<br />
<b>*AutoFixArabicNumbersInFileName*</b>: (flag) Fix by replacing Arabic numbers with English numbers.<br />
<b>*Target*</b>: (text) target directory path where all files will be transferred post-processing..<br />
<b>*Sources*</b>: (array) paths of libraries or files which will be scanned.<br />
<b>*Ignores*</b>: (array) ignore keywords. The program will ignore files that contain a specific keyword in their name or path. You can add folder names, file extensions, or parts of file names to the list of ignores list.<br />
