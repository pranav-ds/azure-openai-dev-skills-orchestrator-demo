# Copyright (c) 2024 Pranav Sumanth Doijode
# 
# This software is released under the MIT License.
# https://opensource.org/licenses/MIT

class Function(dict):
    """
    A class to represent a function.
    """
    def __init__(self, function_name=None, source_code=None, source_file=None):
        """
        Initialize the function.
        
        params:
            function_name (str): The name of the function.
            source_code (str): The source code of the function.
            source_file (str): The source file of the function.
            
        returns:
            None
        """
        super().__init__()
        self['name'] = function_name
        self['source'] = [source_code] # In case of function overloading
        self['sourcefile'] = source_file

    def __str__(self):
        """
        Return a string representation of the function.

        returns:
            str: A string representation of the function.
        """
        return f"Function(name={self['name']}, source={self['source']}, sourcefile={self['sourcefile']})"
    
    def prompt_str(self):
        """
        Return a string representation of the function.

        returns:
            str: A string representation of the function.
        """
        return f"Function {self['name']}:\n, source={self['source']})"

class FunctionStack(dict):
    """
    A class to represent a function stack.
    """
    def __init__(self, function=None):
        """
        
        Initialize the function stack.
        
        params:
            function (Function): The function.
        returns:
            None
        
        """
        super().__init__()
        self['function'] = function
        self['clients'] = []
        self['called_functions'] = []

    def add_client(self, client):
        self['clients'].append(client)

    def add_called_function(self, called_function):
        self['called_functions'].append(called_function)