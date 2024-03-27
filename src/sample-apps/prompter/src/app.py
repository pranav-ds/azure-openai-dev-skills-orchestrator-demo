from flask import Flask

import os
import pathlib

app = Flask(__name__)

@app.post('/api/context')
def home():
    try:
        # Get the absolute path of the current script
        script_location = pathlib.Path(__file__).parent.absolute()
        # Construct the absolute path of the file to read
        file_location = os.path.join(script_location, '..', 'data', 'SensorSuite.cpp')

        with open(file_location, 'r') as file:
            data = file.read()
        return data
    except FileNotFoundError:
        return "File not found"

if __name__ == '__main__':
    app.run(debug=True, port=2020)