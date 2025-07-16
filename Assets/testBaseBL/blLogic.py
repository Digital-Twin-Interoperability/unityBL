from flask import Flask, request, jsonify
import json

app = Flask(__name__)

# Mock database for high scores
coords = []

@app.route('/save_score', methods=['POST'])
def save_score():
    data = request.get_json()
    
    # Add some debugging
    print(f"Received data: {data}")
    
    x = data.get('x')
    y = data.get('y') 
    z = data.get('z')

    # Save coords to mock database
    coords.append({"x": x, "y": y, "z": z})
    return jsonify({"message": "Coords saved successfully!", "coords": coords})

@app.route('/get_scores', methods=['GET'])
def get_scores():
    return jsonify({"coords": sorted(coords, key=lambda x: (x['x'], x['y'], x['z']), reverse=True)})

@app.route('/hsml_representation', methods=['GET'])
def hsml_representation():
    # Represent the routes in HSML-like JSON format
    hsml_json = {
        "version": "1.0",
        "routes": [
            {
                "route": "/save_score",
                "method": "POST",
                "handler": {
                    "action": "save_score",
                    "inputs": {
                        "x": "number",
                        "y": "number",
                        "z": "number"
                    },
                    "outputs": {
                        "message": "string",
                        "coords": [
                            {
                                "x": "number",
                                "y": "number",
                                "z": "number"
                            }
                        ]
                    },
                    "logic": {
                        "debug": "print_received_data",
                        "store_coords": "append_to_coords_array",
                        "response_message": "Coords saved successfully!"
                    }
                }
            },
            {
                "route": "/get_scores",
                "method": "GET",
                "handler": {
                    "action": "get_scores",
                    "outputs": {
                        "coords": [
                            {
                                "x": "number",
                                "y": "number",
                                "z": "number"
                            }
                        ]
                    },
                    "logic": {
                        "sort_coords": {
                            "key": ["x", "y", "z"],
                            "reverse": true
                        }
                    }
                }
            }
        ],
        "database": {
            "coords": [
                {
                    "x": "number",
                    "y": "number",
                    "z": "number"
                }
            ]
        },
        "metadata": {
            "description": "API for saving and retrieving coordinates with sorting functionality.",
            "debug_enabled": True
        }
    }
    
    # Return the HSML-like JSON as a response
    return jsonify(hsml_json)

if __name__ == '__main__':
    app.run(debug=True)
