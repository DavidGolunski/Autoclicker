# Autoclicker (by David Golunski)
A small plugin that allows you to control an Autoclicker from the Streamdeck.  
You can simulate left, middle and right mouse button clicks with a custom delay.


## Troubleshooting
- __The Delay is showing "0":__  
When setting the delay it is important that you input an integer (whole number) between 0 and 9999.   
If you have a "." or a letter inside the number the program will default to the delay to 0

- __The Autoclicker does not work in my Game:__  
Programs and games sometimes try to find out if a mouse click was a "real" mouse click or a simulated one.  
If the game or program really wants to prevent autoclickers it usually will do so :(

- __Log File:__  
The plugin logs any issues inside the log file. You can find the log file at:  
```%appdata%/Elgato/StreamDeck/Plugins/com.davidgolunski.autoclicker.sdPlugin/pluginlog.log```  
If the log file does not help you further you can always ask for help on the [official Discord](https://discord.gg/9qMPNxRhqt).

## Credits and Support
Thank you to [BarRaider](https://barraider.com/) and their [Streamdeck Tools](https://github.com/BarRaider/streamdeck-tools) which allowed quicker and easier development.
Some Icons have been taken from [uxwing](https://uxwing.com/).

If you like the plugin please consider [supporting me via PayPal](https://www.paypal.com/donate/?hosted_button_id=ZN3URG59JBRVJ) and [joining the Discord](https://discord.gg/9qMPNxRhqt).   
This will allow me to keep the applications alive for a little bit longer :)


## Change and Release Notes
### Version 1.0.2
- Added "Support" buttons now link to the Elgato Marketplace. Users can join the discord from the link on the marketplace.

### Version 1.0.1
- Added "Support" buttons to the actions PropertyInspector

### Version 1.0.0
- Implementation of __"Autoclicker Action"__ (Button and Dial)

