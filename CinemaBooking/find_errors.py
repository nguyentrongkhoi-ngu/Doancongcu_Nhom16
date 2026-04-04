import codecs

def find_all_errors(filename, count=20):
    try:
        with codecs.open(filename, 'r', 'utf16', errors='ignore') as f:
            lines = f.readlines()
            found = 0
            for line in lines:
                if 'error' in line.lower():
                    print(line.strip())
                    found += 1
                    if found >= count:
                        break
            if found == 0:
                print("No lines containing 'error' found. Printing first 10 lines of file:")
                for line in lines[:10]:
                    print(line.strip())
    except Exception as e:
        print(f"Error: {e}")

if __name__ == "__main__":
    find_all_errors('build_full.txt')
