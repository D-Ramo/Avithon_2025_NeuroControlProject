package com.avithon.neurocontrolapp;

import com.fasterxml.jackson.databind.DeserializationFeature;
import com.fasterxml.jackson.databind.ObjectMapper;
import javafx.application.Application;
import javafx.fxml.FXMLLoader;
import javafx.scene.Scene;
import javafx.scene.image.Image;
import javafx.stage.Stage;

import java.io.File;
import java.io.IOException;
import java.net.URISyntaxException;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.LinkedBlockingQueue;
import java.util.logging.*;

public class HelloApplication extends Application {
    public static final HashMap<String,LinkedBlockingQueue<String>> queues = new HashMap<>();
    public static final ObjectMapper objectMapper = new ObjectMapper();
    public static final List<MulticastConnection> multicastConnections = new ArrayList<>();
    public static ExecutorService executor = Executors.newFixedThreadPool(10);
    public static Config config;
    public static final Logger LOGGER = Logger.getLogger(HelloApplication.class.getName());

    public static void setupLogger() throws IOException {
        objectMapper.configure(DeserializationFeature.FAIL_ON_UNKNOWN_PROPERTIES, false);
        // Get current working directory
        String currentDir = System.getProperty("user.dir");
        String logFile = Paths.get(currentDir, "app.log").toString();

        // Create a file handler that writes to the log file
        FileHandler fileHandler = new FileHandler(logFile, true); // append = true

        // Set a simple formatter
        fileHandler.setFormatter(new SimpleFormatter());

        // Add to logger
        LOGGER.addHandler(fileHandler);

        // Optional: log to console as well
        ConsoleHandler consoleHandler = new ConsoleHandler();
        consoleHandler.setFormatter(new SimpleFormatter());
        LOGGER.addHandler(consoleHandler);

        // Optional: disable default console logging
//        Logger rootLogger = Logger.getLogger("");
//        rootLogger.setLevel(Level.SEVERE);
//        rootLogger.addHandler(fileHandler);
//        Handler[] handlers = rootLogger.getHandlers();
//        for (Handler h : handlers) {
//            if (h instanceof ConsoleHandler) {
//                rootLogger.removeHandler(h);
//            }
//        }
    }

    @Override
    public void start(Stage stage) {
//        try {
////            setupLogger();
//        } catch (IOException e) {
//            HelloApplication.LOGGER.log(Level.SEVERE,e.getMessage());
//        }
        try {
            objectMapper.configure(DeserializationFeature.FAIL_ON_UNKNOWN_PROPERTIES, false);
            // Leave group and close socket
            FXMLLoader fxmlLoader = new FXMLLoader(HelloApplication.class.getResource("hello-view.fxml"));
            Scene scene = null;
            try {
                scene = new Scene(fxmlLoader.load(), 1280, 800);
            } catch (IOException e) {
                HelloApplication.LOGGER.log(Level.SEVERE,e.getMessage());

            }
            HelloController controller = fxmlLoader.getController();

            stage.setTitle("Neuro Control App");
            stage.setIconified(false);
            stage.setScene(scene);
            stage.setOnCloseRequest(event -> {
                for (MulticastConnection multicastConnection : multicastConnections) {
                    multicastConnection.close();
                }
                for (Stage stage1: controller.activeStages){
                    ((PopupController)stage1.getUserData()).thread.interrupt();
                    stage1.close();
                }
                controller.thread.interrupt();
            });
            Image logo = new Image(getClass().getResourceAsStream("images/logo_16x16.png"));
            stage.getIcons().add(logo);
            loadConfig();
            scene.setOnKeyPressed(event -> {
                if (event.getText().toLowerCase().equals("s")){
                    SettingsController.showConfig(null);
                }
            });
            stage.show();
        }catch (Exception e){
            LOGGER.log(Level.SEVERE,e.getMessage(),e);
        }



    }

    public static void main(String[] args) {
        launch(args);
    }

    public static void loadConfig(){
        try {
            File dir = new File(HelloApplication.class.getProtectionDomain().getCodeSource().getLocation().toURI()).getParentFile().getParentFile();
            File file = new File(dir, "config.json");
            if (!file.exists()){
                SettingsController.showConfig(null);
                return;
            }
            String configStr = new String(Files.readAllBytes(file.toPath()));
            ObjectMapper objectMapper = new ObjectMapper();
            if (configStr == null || configStr.isEmpty()){
                SettingsController.showConfig(null);
                return;
            }
            config = objectMapper.readValue(configStr, Config.class);

        } catch (IOException e) {
            HelloApplication.LOGGER.log(Level.SEVERE,e.getMessage());

        } catch (URISyntaxException e) {
            HelloApplication.LOGGER.log(Level.SEVERE,e.getMessage());
        }
    }
}