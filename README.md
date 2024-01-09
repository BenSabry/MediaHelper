# Media Organizer
<b>*Media Organizer*</b> is a tool engineered to assist in arranging your media files into the <b>*/Library/Year/Month/file*</b> structure, based on their creation date. It also aids in resolving any problems related to media file organization <b>*[Issues like these](#Fixable-Issues)*</b>.

# Downloads
<b>*Latest release*</b> [v0.5.2-alpha](https://github.com/BenSabry/MediaOrganizer/releases/tag/v0.5.2-alpha)<br />
<b>*All releases*</b> [releases](https://github.com/BenSabry/MediaOrganizer/releases)

# Story
I’m a proud owner of a Synology (DS720+) and I must say, I’m quite fond of it, especially the Photos/Drive apps.<br />

Previously, I was a Google Photos user, so I downloaded a complete backup of all my media from Google and transferred it to the Synology Photos folder. However, I encountered a significant issue - the order of the media. All my media was arranged according to the date I uploaded it to Synology, not the creation date or date taken, etc. After some research, I found numerous online discussions about this issue, even Synology Photos’ solution of manually modifying the image date is good but not feasible for a batch of more than 20,000 files!<br />

So, I decided to tackle this problem head-on and develop my own solution. After several days of development and testing on my own media library, I’m happy to report that it’s complete and all my files are now properly sorted on Synology Photos.<br />

Why am I sharing this? Because I experienced this frustrating problem and I want to help others who might be facing the same issue. If you’re interested, you can try it out and share your feedback.<br />

Please remember to BACKUP your media before trying anything new, whether it’s from me or anyone else. Lastly, a big thank you to Synology for the excellent combination of software and hardware. I truly enjoy your product.

# Notice
This tool is compatible with <b>*Synology, Qnap*</b>, and other NAS solutions, as well as local media files. If your media is scattered across multiple sources, folders, or locations, and you wish to consolidate and organize them in one place, this tool is ideal. It also supports <b>*Google Takeout*</b> backups and archived zip files. Simply add these to the sources section in <b>*[AppSettings.json](#AppSettings)*</b>.

# Recommendations
a. <b>*BACKUP*</b> your media files first.<br />
b. Increase the <b>*TasksCount*</b> in <b>*[AppSettings.json](#AppSettings)*</b> (recommended: 2, best: <b>*CPU Cores*</b> count)<br />

# How to use
1. Add the path of your library to <b>*Sources*</b> and <b>*Target*</b> in <b>*[AppSettings.json](#AppSettings)*</b> file<br />
2. run the executable <b>*MediaOrganizer.exe*</b> and wait<br />

# How it works
1. Scan library files and directories added to <b>*Sources*</b> in <b>*[AppSettings.json](#AppSettings)*</b><br />
2. Extract all available dates from the metadata of a file (such as TakenDate, CreateDate, etc.) and from the filename, using a wide range of supported datetime format patterns.
3. Select the oldest valid datetime.
4. Copy the file to proper directory <b>*Target\Year\Month\File.*</b> based on oldest datetime.<br />
5. Update the new file info <b>*TakenDate*</b>/<b>*CreationDate*</b>
6. Attempt to fix file info (like duplications/incorrect offsets etc.)<br />
7. Remove any empty directories or temporary backups that were created by the program.<br /><br />


# Fixable Issues
[<b>*Synology Photos: Not Using Taken Date*</b>](https://www.reddit.com/r/synology/comments/kgy604/synology_photos_not_using_taken_date/)<br />
[<b>*Synology Photos: organizes everything by modified date instead of creation date*</b>](https://www.reddit.com/r/synology/comments/120jsvk/synology_photos_organizes_everything_by_modified/)<br />
[<b>*Synology Photos: Indexing photos/videos with wrong date*</b>](https://www.reddit.com/r/synology/comments/qj9wya/synology_photos_indexing_photosvideos_with_wrong/)<br />
[<b>*Synology Photos: Best practice for photos with no taken date*</b>](https://www.reddit.com/r/synology/comments/rn5cvm/best_practice_for_photos_with_no_taken_date/)<br />

# Tech/Tools used
<b>*.NET*</b>: is the free, open-source, cross-platform framework for building modern apps and powerful cloud services.<br />
<b>*ExifTool*</b>: is a customizable set of Perl modules plus a full-featured command-line application for reading and writing meta information in a wide variety of files.<br />

# AppSettings
<b>*TasksCount*</b>: (number) of <b>*Tasks/Threads*</b> to work simultaneously.<br />
<b>*EnableLogAndResume*</b>: (flag) to ensure continuity and avoid repetition, <br />
&nbsp;&nbsp;&nbsp;&nbsp;actions are recorded in the log.txt files located in <b>*.\Temp\Log.*</b> <br />
&nbsp;&nbsp;&nbsp;&nbsp;This allows for resumption from the exact point where you last left off, rather than starting anew.<br />
<b>*AttemptToFixMediaIncorrectOffsets*</b>: (flag) to fix file info (like duplications/incorrect offsets etc.)<br />
<b>*ClearBackupFilesOnComplete*</b>: (flag) Clear temp files on complete.<br />
<b>*DeleteEmptyDirectoriesOnComplete*</b>: (flag) Delete empty directories on complete.<br />
<b>*Target*</b>: (text) target directory path where all files will be transferred post-processing..<br />
<b>*Sources*</b>: (array) paths of libraries or files which will be scanned.<br />
<b>*Ignores*</b>: (array) ignore keywords. The program will ignore files that contain a specific keyword in their name or path. You can add folder names, file extensions, or parts of file names to the list of ignores list.<br />

# AppSettings Example
{<br />
&nbsp;&nbsp;"TasksCount": 2,<br />
&nbsp;&nbsp;"EnableLogAndResume": true,<br />
&nbsp;&nbsp;"AttemptToFixMediaIncorrectOffsets": true,<br />
&nbsp;&nbsp;"ClearBackupFilesOnComplete": true,<br />
&nbsp;&nbsp;"DeleteEmptyDirectoriesOnComplete": true,<br />
&nbsp;&nbsp;"Target": "\\\\SynologyNAS\\home\\Photos\\PhotoLibrary",<br />
&nbsp;&nbsp;"Sources": [<br />
&nbsp;&nbsp;&nbsp;&nbsp;"\\\\SynologyNAS\\home\\Google-Takeout.zip",<br />
&nbsp;&nbsp;&nbsp;&nbsp;"\\\\SynologyNAS\\home\\Photos\\MobileBackup",<br />
&nbsp;&nbsp;&nbsp;&nbsp;"D:\\Data\\Media\\Photos\\Personal\\Family",<br />
&nbsp;&nbsp;&nbsp;&nbsp;"\\\\SynologyNAS\\home\\GraduationPhoto.jpg",<br />
&nbsp;&nbsp;&nbsp;&nbsp;"\\\\SynologyNAS\\home\\GraduationVideo.mp4",<br />
&nbsp;&nbsp;],<br />
&nbsp;&nbsp;"Ignores": [<br />
&nbsp;&nbsp;&nbsp;&nbsp;".avi",<br />
&nbsp;&nbsp;&nbsp;&nbsp;"PersonalVideos",<br />
&nbsp;&nbsp;],<br />
}<br />
