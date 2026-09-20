pipeline {
    agent any
    stages {
        stage('Checkout') {
            steps { checkout scm }
        }
        stage('Build') {
            steps {
                bat '"C:\\Program Files\\Unity\\Hub\\Editor\\2022.3.76f1\\Editor\\Unity.exe" -quit -batchmode -projectPath "%WORKSPACE%" -executeMethod BuildAutomator.Build -logFile Builds/build.log'
            }
        }
    }
}
