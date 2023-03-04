import type { ForgeConfig } from '@electron-forge/shared-types';
import { MakerSquirrel } from '@electron-forge/maker-squirrel';
import { MakerZIP } from '@electron-forge/maker-zip';
import { MakerDeb } from '@electron-forge/maker-deb';
import { MakerRpm } from '@electron-forge/maker-rpm';
import { MakerAppImage } from '@reforged/maker-appimage'
import { WebpackPlugin } from '@electron-forge/plugin-webpack';
import { mainConfig } from './webpack.main.config';
import { rendererConfig } from './webpack.renderer.config';
import PortableMaker from "./src/infrastructure/PortableMaker";
import fs from 'fs';

const config: ForgeConfig = {
  packagerConfig: {
    icon: "./assets/appicon",
    beforeCopyExtraResources: [
      (buildPath, electronVersion, platform, arch, callback) => {
        if (!fs.existsSync("./artifacts")) {
          fs.mkdirSync("./artifacts");
        }
        if (!fs.existsSync("./artifacts/ControlR_Sidecar.exe")){
          fs.createWriteStream("./artifacts/ControlR_Sidecar.exe").close();
        }

        if (!fs.existsSync("./artifacts/ControlR_Sidecar")){
          fs.createWriteStream("./artifacts/ControlR_Sidecar").close();
        }

        callback();
      }
    ],
    extraResource: [
      "./artifacts/ControlR_Sidecar.exe",
      "./artifacts/ControlR_Sidecar",
      "./assets/appicon.icns",
      "./assets/appicon.ico",
      "./assets/appicon.png"
    ]
  },
  rebuildConfig: {},
  makers: [
    new MakerSquirrel({
      setupIcon: "./assets/appicon.ico",
    }),
    new PortableMaker({
       icon: "./assets/appicon.ico",
       win: {
        artifactName: "ControlR.exe",
        icon: "./assets/appicon.ico",
        target: "portable"
       }
    }),
    new MakerZIP({}, ['darwin', 'win32', 'linux']), 
    new MakerRpm({
      options: {
        icon: "./assets/assets/appicon.png"
      }
    }),
    new MakerAppImage({ 
      options: {
        icon: "./assets/assets/appicon.png",
        categories: [ "Utility" ],
    }}),
    new MakerDeb({
      options: {
        icon: "./assets/assets/appicon.png"
      }
    })],
  plugins: [
    new WebpackPlugin({
      mainConfig,
      renderer: {
        config: rendererConfig,
        entryPoints: [
          {
            html: './src/renderer/index.html',
            js: './src/renderer/renderer.tsx',
            name: 'main_window',
            preload: {
              js: './src/renderer/preload.ts',
            },
          },
        ],
      },
    }),
  ],
};

export default config;
