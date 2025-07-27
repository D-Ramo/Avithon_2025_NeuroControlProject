package com.avithon.neurocontrolapp;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import javafx.application.Platform;
import javafx.event.Event;
import javafx.fxml.FXML;
import javafx.fxml.FXMLLoader;
import javafx.fxml.Initializable;
import javafx.scene.Parent;
import javafx.scene.Scene;
import javafx.scene.control.TextField;
import javafx.stage.Modality;
import javafx.stage.Stage;

import java.io.File;
import java.io.FileWriter;
import java.io.IOException;
import java.net.URISyntaxException;
import java.net.URL;
import java.util.ResourceBundle;
import java.util.logging.Level;

public class SettingsController implements Initializable {
    @FXML
    private TextField tfApiIp;

    @FXML
    private TextField tfApiPort;

    @FXML
    private TextField tfMulticastGroup;

    @FXML
    private TextField tfMulticastIp;

    @FXML
    private TextField tfMulticastPort;

    static Stage stage;
    static Stage caller;

    public static boolean isShowing = false;

    static SettingsController settingsController;


    @FXML
    protected void saveConfig(Event event){
        Config config = new Config();
        config.apiIp = tfApiIp.getText().trim();
        config.apiPort = tfApiPort.getText().trim();
        config.multicastGroup = tfMulticastGroup.getText().trim();
        config.multicastIp = tfMulticastIp.getText().trim();
        config.multicastPort = tfMulticastPort.getText().trim();

        ObjectMapper objectMapper = new ObjectMapper();

        try {
            File dir = new File(HelloApplication.class.getProtectionDomain().getCodeSource().getLocation().toURI()).getParentFile().getParentFile();
            config.setAdditional(dir.getAbsolutePath());
            File file = new File(dir, "config.json");
            String conf = objectMapper.writeValueAsString(config);
            FileWriter fileWriter = new FileWriter(file);
            fileWriter.write(conf);
            fileWriter.flush();
            fileWriter.close();
            HelloApplication.loadConfig();
            if (caller!=null){
                caller.close();
            }
            stage.close();

        } catch (JsonProcessingException e) {
            HelloApplication.LOGGER.log(Level.SEVERE,e.getMessage());

//            throw new RuntimeException(e);
        } catch (IOException | URISyntaxException e) {
            HelloApplication.LOGGER.log(Level.SEVERE,e.getMessage());

        }

    }

    public static void showConfig(Stage caller){
        isShowing = true;
        SettingsController.caller = caller;
        Platform.runLater(()->{
            FXMLLoader loader = new FXMLLoader(SettingsController.class.getResource("settings.fxml"));
            Parent root = null;
            try {
                root = loader.load();
            } catch (IOException e) {
                HelloApplication.LOGGER.log(Level.SEVERE,e.getMessage());

            }
            stage = new Stage();
            stage.initModality(Modality.APPLICATION_MODAL); // block events to other windows
            stage.setTitle("Settings");
            stage.setIconified(false);
            stage.setScene(new Scene(root));
            stage.showAndWait();

            isShowing = false;
        });

    }

    @Override
    public void initialize(URL url, ResourceBundle resourceBundle) {
        settingsController = this;
        if (HelloApplication.config!=null){
            tfApiIp.setText(HelloApplication.config.apiIp);
            tfApiPort.setText(HelloApplication.config.apiPort);
            tfMulticastGroup.setText(HelloApplication.config.multicastGroup);
            tfMulticastIp.setText(HelloApplication.config.multicastIp);
            tfMulticastPort.setText(HelloApplication.config.multicastPort);
        }
    }
}
