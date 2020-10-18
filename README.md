# b2clone

A simple CLI tool to push local folders and files to Backblaze B2 cloud.

## Why create b2clone?

This is my initiative to create my own Backblaze B2 client instead of using third party / open source tools. I used rclone before, which is impressively good but I accidently deleted my B2 script. Instead of creating another script, I create my own tool to push my data to B2 cloud which I can customize whatever I want it to be. 😁 

## Disclaimer

*Use at your own risk. I'm not responsible if anything happens to your local files, B2 files, your cat, your mother, etc.*

## Screenshot

![Screenshot of b2clone](https://f001.backblazeb2.com/file/public-cloud/Pictures/Github/b2clone/Screenshot+2020-10-18+213809.png)

![User configuration](https://f001.backblazeb2.com/file/public-cloud/Pictures/Github/b2clone/Screenshot+2020-10-18+213908.png)

## Features

* It should be fast 🤣
* Supposedly works on other OSes, but I only tested on my Windows machine
* Using [xxHash](https://github.com/Cyan4973/xxHash) for file hashing

## To-do

* Add support for local file removal detection, and put those file in your bucket trash bin.
* Make the journal (embedded database) file size smaller.
* Files encryption
* Graphical user interface (GUI) - This is my original idea, but it does take a lot of time to do this. So I just make CLI tool first. 😔

## How to use

1. Run b2clone (to generate the user configuration file = *user.conf.txt*)
2. Edit the configuration file as required.
	* *PathMapper* maps your bucket path to your local path. Left side is your bucket path, right side is your local path.
	* Remember `\\` for your local path.
3. Save your configuration file and run b2clone again.

## How to build

### Windows

1. You should have dotnet core 3.x
2. Clone this repo or download this as zip - Your choice
3. Go to `b2clone\b2clone`
4. Type `dotnet  build --configuration Release`
5. Compiled files will be generated at `bin\Release`.

### Linux/MacOs

* Should be same like Windows but **I do not test or run this tool all on these OSes**. It might be broken.

---
b2clone - Ijat.my
