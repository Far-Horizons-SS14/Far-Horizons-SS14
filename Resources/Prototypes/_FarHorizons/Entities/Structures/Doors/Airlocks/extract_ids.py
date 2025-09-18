import re
import argparse

def get_entity_ids_regex(yaml_file):
    """
    Reads a file and extracts 'id' values using a regular expression.

    Args:
        yaml_file (str): The path to the YAML file.

    Returns:
        list: A list of 'id' values found.
    """
    # Regex pattern to match 'id:' followed by an identifier
    # \s* matches any whitespace
    # (.+) captures one or more characters on the same line
    pattern = re.compile(r'id:\s*(.+)')
    
    ids = []
    try:
        with open(yaml_file, 'r') as file:
            for line in file:
                match = pattern.search(line)
                if match:
                    # The captured group is at index 1
                    ids.append(match.group(1).strip())
    except FileNotFoundError:
        print(f"Error: The file '{yaml_file}' was not found.")
        return []
    
    return ids

def main():
    """
    Parses command-line arguments and prints a list of entity IDs.
    """
    parser = argparse.ArgumentParser(description="Lists entity IDs from a YAML file using regex.")
    parser.add_argument("yaml_file", type=str, help="The path to the YAML file.")
    args = parser.parse_args()

    entity_ids = get_entity_ids_regex(args.yaml_file)
    if entity_ids:
        for entity_id in entity_ids:
            print(entity_id)
    else:
        print(f"No 'id' values found in the YAML file: {args.yaml_file}")

if __name__ == "__main__":
    main()