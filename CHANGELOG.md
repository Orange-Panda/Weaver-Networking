# Changelog
All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/) and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.2.2] - 2021-04-04

### Changed
- NetworkCore variable accessibility has been modified to be a bit more secure.

### Fixed
- Trying to connect to an invalid as a client will no longer break functionality of the network core.

## [0.2.1] - 2021-04-03

### Changed
- Network Core instantiates using a string key lookup instead of network contract index.
	- The Network Contract will still reference the player via index.

### Fixed
- Editor will not longer send a warning when exiting playmode.

## [0.2.0] - 2021-03-30

### Added
- Launch options:
	- -scene "sceneName"
	- -server
	- -serverCam
	- -ip "address"
	- -port "port"

### Changed
- Network component has dirty built in
- Network components have unique ids to prevent mixed messages on game objects with separate network scripts.

## [0.1.1] - 2021-03-30

### Changed
- The namespace has been renamed from "NetworkEngine" to "LMirman.Weaver"

## [0.1.0] - 2021-03-22

### Added
- The Network Engine has been uploaded.