import { app, BrowserWindow, screen, session } from "electron";
import appState from "./services/appState";
import { getAssetsPath } from "./services/fsHelpers";
import { registerIpcHandlers } from "./services/ipcMainHandlers";
import { cleanupLogs, writeLog } from "./services/logger";
import { watchClipboard } from "./services/clipboardManager";
import { launchSidecar } from "./services/sidecarIpc";
import { watchForDisplayChanges } from "./services/mediaHelperMain";

// This allows TypeScript to pick up the magic constants that's auto-generated by Forge's Webpack
// plugin that tells the Electron app where to look for the Webpack-bundled app code (depending on
// whether you're running in development or production).
declare const MAIN_WINDOW_WEBPACK_ENTRY: string;
declare const MAIN_WINDOW_PRELOAD_WEBPACK_ENTRY: string;

// Handle creating/removing shortcuts on Windows when installing/uninstalling.
if (require("electron-squirrel-startup")) {
  app.quit();
}

const createMainWindow = (): void => {
  writeLog("Creating main window.  App State: ", "Info", appState);

  const windowWidth = appState.isUnattended ? 300 : 800;
  const windowHeight = appState.isUnattended ? 100 : 600;
  const titleBarStyle = appState.isUnattended ? "hidden" : "default";

  // Create the browser window.
  const mainWindow = new BrowserWindow({
    height: windowHeight,
    width: windowWidth,
    show: !appState.isUnattended || appState.notifyUser,
    title: "ControlR",
    titleBarStyle: titleBarStyle,
    icon: getAssetsPath() + "/appicon.png",
    webPreferences: {
      preload: MAIN_WINDOW_PRELOAD_WEBPACK_ENTRY,
    },
  });
  // and load the index.html of the app.
  mainWindow.loadURL(MAIN_WINDOW_WEBPACK_ENTRY);

  if (appState.isDev) {
    // Open the DevTools.
    mainWindow.webContents.openDevTools();
    mainWindow.setSize(1600, 900);
  } else {
    mainWindow.setMenuBarVisibility(false);
  }

  if (appState.isUnattended && !appState.notifyUser) {
    mainWindow.hide();
  }
  if (appState.isUnattended && !appState.isDev) {
    const currentScreen = screen.getDisplayMatching(mainWindow.getBounds());
    mainWindow.setPosition(
      currentScreen.workArea.width - windowWidth,
      currentScreen.workArea.height - windowHeight,
    );
  } else {
    mainWindow.center();
  }
};

// This method will be called when Electron has finished
// initialization and is ready to create browser windows.
// Some APIs can only be used after this event occurs.
app.on("ready", async () => {
  //if (!appState.isUnattended) {
  //  writeLog("Attended sessions are not implemented.", "Error");
  //  app.exit();
  //  return;
  //}

  try {
    await launchSidecar();
  } catch (e) {
    writeLog(
      "Failed to launch and connect to sidecar process. Exiting.",
      "Error",
      e,
    );
    app.exit();
    return;
  }
  registerIpcHandlers();
  cleanupLogs();
  watchClipboard();
  watchForDisplayChanges();

  setCspHandler();
  createMainWindow();
});

// Quit when all windows are closed, except on macOS. There, it's common
// for applications and their menu bar to stay active until the user quits
// explicitly with Cmd + Q.
app.on("window-all-closed", () => {
  if (process.platform !== "darwin") {
    app.quit();
  }
});

app.on("activate", () => {
  // On OS X it's common to re-create a window in the app when the
  // dock icon is clicked and there are no other windows open.
  if (BrowserWindow.getAllWindows().length === 0) {
    createMainWindow();
  }
});

// Unsafe-eval required for WebPack.
function setCspHandler() {
  session.defaultSession.webRequest.onHeadersReceived((details, callback) => {
    callback({
      responseHeaders: {
        ...details.responseHeaders,
        "Content-Security-Policy": [
          "default-src 'self' 'unsafe-inline' 'unsafe-eval' " +
            `${appState.serverUri} ${appState.websocketUri} data: https://fonts.googleapis.com; ` +
            "font-src https://fonts.googleapis.com https://fonts.gstatic.com;",
        ],
      },
    });
  });
}

// In this file you can include the rest of your app's specific main process
// code. You can also put them in separate files and import them here.
